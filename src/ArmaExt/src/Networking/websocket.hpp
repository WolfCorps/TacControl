#include <boost/beast/core.hpp>
#include <boost/beast/websocket.hpp>
#include <thread>

#include <boost/beast/http.hpp>
#include <boost/beast/version.hpp>
#include <boost/asio/ip/tcp.hpp>
#include <boost/asio/ip/udp.hpp>
#include <boost/config.hpp>
#include <json.hpp>
#include <memory>
#include <string>
#include <thread>
#include <vector>
#include <filesystem>
#include <mutex>
#include <unordered_set>
#include <variant>

#include "Util/SignalSlot.hpp"

using json = nlohmann::json;
using tcp = boost::asio::ip::tcp;               // from <boost/asio/ip/tcp.hpp>
using udp = boost::asio::ip::udp;               // from <boost/asio/ip/udp.hpp>
namespace http = boost::beast::http;            // from <boost/beast/http.hpp>
namespace beast = boost::beast;            // from <boost/beast/http.hpp>
namespace websocket = boost::beast::websocket;  // from <boost/beast/websocket.hpp>
namespace net = boost::asio;            // from <boost/asio.hpp>


class http_session;


// Report a failure
void fail(boost::system::error_code ec, char const* what);





// Forward declaration
class websocket_session;

// Represents the shared server state
class shared_state
{
    std::string const doc_root_;

    // This mutex synchronizes all access to sessions_
    std::mutex mutex_;

    // Keep a list of all the connected clients
    std::unordered_set<websocket_session*> sessions_;

    std::shared_ptr<nlohmann::json> currentState;
public:
    explicit
        shared_state(std::string doc_root);

    std::string const&
        doc_root() const noexcept
    {
        return doc_root_;
    }

    void join(websocket_session* session);
    void leave(websocket_session* session);
    //void send(std::string message);
    void updateState(const nlohmann::json&& newState);


    Signal<void(std::string, boost::shared_ptr<websocket_session>)> OnMessage;
};

class http_session : public boost::enable_shared_from_this<http_session>
{
    beast::tcp_stream stream_;
    beast::flat_buffer buffer_;
    boost::shared_ptr<shared_state> state_;

    // The parser is stored in an optional container so we can
    // construct it from scratch it at the beginning of each new message.
    boost::optional<http::request_parser<http::string_body>> parser_;

    void fail(beast::error_code ec, char const* what);
    void do_read();
    void on_read(beast::error_code ec, std::size_t);
    void on_write(beast::error_code ec, std::size_t, bool close);

public:
    http_session(
        tcp::socket&& socket,
        boost::shared_ptr<shared_state> const& state);

    void run();
};


/** Represents an active WebSocket connection to the server
*/
class websocket_session : public boost::enable_shared_from_this<websocket_session>
{
    beast::flat_buffer buffer_;
    websocket::stream<beast::tcp_stream> ws_;
    boost::shared_ptr<shared_state> state_;
    std::vector<boost::shared_ptr<std::variant<const std::string, const std::vector<uint8_t>>>> queue_;

    void fail(beast::error_code ec, char const* what);
    void on_accept(beast::error_code ec);
    void on_read(beast::error_code ec, std::size_t bytes_transferred);
    void on_write(beast::error_code ec, std::size_t bytes_transferred);

public:

    using MessageType = std::variant<const std::string, const std::vector<uint8_t>>;


    websocket_session(
        tcp::socket&& socket,
        boost::shared_ptr<shared_state> const& state);

    ~websocket_session();

    template<class Body, class Allocator>
    void run(http::request<Body, http::basic_fields<Allocator>> req);

    // Send a message
    void send(boost::shared_ptr<MessageType> const& ss);
    void send(const nlohmann::json& jsonMessage);

    std::shared_ptr<nlohmann::json> lastState;

    enum class JsonType {
        plainText,
        BSON,
        CBOR,
        MsgPack,
        UBJSON
    };

    JsonType jsonType = JsonType::plainText;


private:
    void on_send(boost::shared_ptr<MessageType> const& ss);
};

template<class Body, class Allocator>
void
websocket_session::
run(http::request<Body, http::basic_fields<Allocator>> req)
{
    // Set suggested timeout settings for the websocket
    ws_.set_option(
        websocket::stream_base::timeout::suggested(
            beast::role_type::server));

    // Set a decorator to change the Server of the handshake
    ws_.set_option(websocket::stream_base::decorator(
        [](websocket::response_type& res)
        {
            res.set(http::field::server,
                std::string(BOOST_BEAST_VERSION_STRING) +
                " websocket-chat-multi");
        }));


    auto enc = req[http::field::accept_encoding];
    if (enc == "CBOR") {
        jsonType = JsonType::CBOR;
    } else if (enc == "BSON") {
        jsonType = JsonType::BSON;
    } else if (enc == "MsgPack") {
        jsonType = JsonType::MsgPack;
    } else if (enc == "UBJSON") {
        jsonType = JsonType::UBJSON;
    }

    // Accept the websocket handshake
    ws_.async_accept(
        req,
        beast::bind_front_handler(
            &websocket_session::on_accept,
            shared_from_this()));
}

//------------------------------------------------------------------------------

// Accepts incoming connections and launches the sessions
class listener : public boost::enable_shared_from_this<listener>
{
    net::io_context& ioc_;
    tcp::acceptor acceptor_;
    boost::shared_ptr<shared_state> state_;

    void fail(beast::error_code ec, char const* what);
    void on_accept(beast::error_code ec, tcp::socket socket);

public:
    listener(
        net::io_context& ioc,
        tcp::endpoint endpoint,
        boost::shared_ptr<shared_state> const& state);

    // Start accepting incoming connections
    void run();
};

// UDP broadcast receive and reply
class UDPBroadcastHost : public boost::enable_shared_from_this<UDPBroadcastHost>
{
    udp::socket socket_;
    udp::endpoint remote_endpoint_;
    std::array<char, 1> recv_buffer_;
    std::string _clientID;
public:
    UDPBroadcastHost(
        net::io_context& ioc,
        udp::endpoint endpoint);

    void doReceive();

    // Start accepting incoming connections
    void run();
};


class Server {

public:
    Server();

    boost::shared_ptr<UDPBroadcastHost> udpBroadcast_;

    boost::shared_ptr<shared_state> state_;
    boost::asio::io_context ioc{ 1 };
    std::vector<std::thread> iothreads;
    boost::shared_ptr<listener> httpServ;
};

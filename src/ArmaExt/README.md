





## Streams

Streams are a alternative method to receive data from the game.
As opposed to state, Stream's content is not stored.
Stream's forward data directly from the game to the client. Without processing the data.

Streams are useful for fast/very frequent update of state, especially state that only few clients are interested in.

Streams can be requested by the client via NetMessage Core.Register
Stream names must begin with "S_"
The Stream's content definition is set in SQF code.

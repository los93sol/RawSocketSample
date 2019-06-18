This project was initially put together to demo the AF_Packet support in .NET Core (as of 3.0 Preview 6)

It consists of a socket server and some clients, simulated as being local on the machine and on a remote machine via docker-compose

I'm currently trying to figure out how to do the following...
1. Set BPF filter
2. Enable PACKET_FANOUT across multiple threads

If you're interested in this little experiment please toss some PR's my way ;)

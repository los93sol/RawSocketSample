This project was initially put together to demo the AF_Packet support in .NET Core (as of 3.0 Preview 6)

What's working now?
1. Socket server to simulate an application with both local and remote clients
2. Number of local and remote clients can be scaled up/down by changing the int to the number you want
3. Packet capture threads are explicitly bound to a given interface
4. Packet capture threads per interface can be scaled up/down by changing the int to the number you want
5. Inbound/Outbound capture on local interface
6. Inbound/Outbound capture on network interface
7. Packet fanout, specifically hash mode for capturing across multiple threads
8. BPF Filtering (hardcoded pseudo asm, but proves the concept)

What's in the works?
1. Generate BPF pseudo asm
2. Experiment with using MMAP and bypassing .NET's ReceiveAsync.  The idea is that the kernel can just shove the packets straight into our userspace without it having to do it's copy before we get it.

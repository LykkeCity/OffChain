## Offchain

This repository is created to implement the ideas for increasing performance of Open Assets transactions in the exchange using Bitcoin network as an arbitration layer, with major ideas comming from Lightning.network .

# Architecture

The idea will be implemented as a network of peer-to-peer bidirectional asset channels, the channels will be setup using a transaction on Bitcoin network (on-chain), but will be updated between nodes off the Bitcoin network and in the case of requirement for a example a dispute, Bitcoin network will be used to settle that requirement. Transfer of an asset between nodes will be performed by one or more channels being updated.

# Deployment

Currently in order to deploy the daemon into node, a sql server instance should be deployed and the database script from OffchainNodeLib\DB should be run to create the corresponding tables and their data.

# Control Requests

The code is still under active development. Control requests are available in Control.cs file of the OffchainNodeLib project under RPC directory.

Some of the control calls are:

*   Create New Channel

*   Reset Channel

*   Negociate Channel

Channel are a path of communication between two nodes, and each node can have channels ending to various nodes.

# Manually Testing

In order to manually test the communication mechanism between two nodes, at lease two instances of OffchainServer.exe daemon should be running, in order to make things easy after each compilation, there is a macro present in the project (Accessible by right clicking the project name in VS 2015 and selecting Edit OffchainServer.csproj) named OutputTestDir , in the same directory under directory Settings a file named OffchainServer.exe.config should be created representing the configuration for one the two nodes participating in test.

# Overview

This repository contains instructions on how to install and run Zeebe with a set of demo workers.

For a more in-depth understanding of Zeebe, please consult the [product documentation](https://docs.zeebe.io/introduction/index.html).

# Getting started

## Prerequisites

1. [Git](https://git-scm.com/downloads)
1. [Docker for Desktop](https://www.docker.com/products/docker-desktop)
1. [.NET Core SDK 2.1](https://dotnet.microsoft.com/download)

## Usage

Clone this repository to your local machine:

## Start Zeebe

The `docker-compose.yml` file in the `zeebe/operate-simple-monitor` folder can be used to start a single Zeebe brokers; optionally with Simple Monitor, and/or with Operate, along with the Elasticsearch and Kibana containers.

Start the containers in the background:

```
# cd to zeebe/operate-simple-monitor
> docker-compose up -d
```

The containers expose the following services:

- Zeebe broker - port 26500
- Operate - web interface http://localhost:8080 (login: demo/demo)
- ElasticSearch - port http://localhost:9200
- Kibana - port http://localhost:5601
- Simple Monitor - web interface http://localhost:8082

Check that your Zeebe is up and running:

Windows:

```
# cd to zeebe folder
> .\cli\zbctl.exe --insecure status
```

MacOS:

```
# cd to zeebe folder
$ ./cli/zbctl.darwin --insecure status
```

This is what you should expect as a result when running a single node:

```
Cluster size: 1
Partitions count: 1
Replication factor: 1
Gateway version: 0.23.1
Brokers:
  Broker 0 - 192.168.144.3:26501
    Version: 0.23.1
    Partition 1 : Leader
```

If it's not yet started, this is what to expect:

```
Error: rpc error: code = Unavailable desc = connection closed
```

For more information on using Zeebe and Operate, consult the Quickstart Guide in the [Zeebe docs](https://docs.zeebe.io/getting-started/index.html).

## Deploy a workflow

Windows:

```
# cd to zeebe folder
> .\cli\zbctl.exe --insecure deploy ..\resources\create-isk-process.bpmn
```

MacOS:

```
# cd to zeebe folder
$ ./cli/zbctl.darwin --insecure deploy ../resources/create-isk-process.bpmn
```

This is what you should expect as a result:

```
{
  "key": 2251799813685250,
  "workflows": [
    {
      "bpmnProcessId": "create-isk-process",
      "version": 1,
      "workflowKey": 2251799813685249,
      "resourceName": "../zeebe-demo/resources/create-isk-process.bpmn"
    }
  ]
}
```

## Run a workflow

The `dotnet run 1` command will create an instance where 1 is used as the customer id and correlation id of the instance.

Run this command:

```
# cd to client folder
> dotnet run 1
```

## Operate a workflow

1. Open the web interface http://localhost:8080 (login: demo/demo)
2. Click on running instances tab and then click on the running instance from the list.

![bild](/img/Operate.png)

## Run the workers

Start the workers one by one and monitor the workflow progress in Operate.

Worker 1 - Create ISK Account:

```
# cd to worker1 folder
> dotnet run normal
```

Worker 2 - Activate ISK Account:

```
# cd to worker2 folder
> dotnet run normal
```

Worker 3 - Create Fund Allocation:

```
# cd to worker3 folder
> dotnet run normal
```

Worker 4 - Create Recurring Transfer:

```
# cd to worker4 folder
> dotnet run normal
```

Worker 5 - Create Monthly Saving:

```
# cd to worker5 folder
> dotnet run normal
```

## Stop Zeebe

To stop the containers and clean the persistent data:

```
> docker-compose down -v
```

## Findings

- Not possible to use same worker mulitple time in a process.
- Can't pause workflow partially by worker execution.

# Overview

This repository contains instructions on how to install and run Zeebe with a set of demo workers.

For a more in-depth understanding of Zeebe, please consult the [product documentation](https://docs.zeebe.io/introduction/index.html).

# Table of Content

- [Getting started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Usage](#usage)
  - [Start Zeebe](#start-zeebe)
  - [Deploy a workflow](#deploy-a-workflow)
  - [Run a workflow](#run-a-workflow)
  - [Operate a workflow](#operate-a-workflow)
  - [Run the workers](#run-the-workers)
  - [Stop Zeebe](#stop-zeebe)
- [Error handling](#error-handling)
  - [Inaccessible backend service I](#inaccessible-backend-service-i)
  - [Exception handling in workflow](#exception-handling-in-workflow)
  - [Worker instance crashes](#worker-instance-crashes)
  - [Unexpected data error](#unexpected-data-error)
  - [Inaccessible backend service II ](#inaccessible-backend-service-ii)
- [Fault tolerance](#fault-tolerance)
- [Horizontal scaling](#horizontal-scaling)
- [Workflow monitoring](#workflow-monitoring)
- [Workflow modelling](#workflow-modelling)
- [Workflow debugging](#workflow-debugging)
- [Develop a worker](#develop-a-worker)
  - [C#](#create-a-worker-in-cs)
  - [Go](#create-a-worker-in-go)
  - [Java](#create-a-worker-in-java)
- [Zeebe and Kafka](#zeebe-and-kafka)
- [Zeebe and Kubernetes](#zeebe-and-kubernetes)

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

[back to toc](#table-of-content)

# Error handling

To run these tests, first [start](#start-zeebe) the Zeebe Broker, [deploy](#deploy-a-workflow) a workflow and [run](#run-the-workers) the workers.

## Inaccessible backend service I

This scenario will test if the workflow can retry sending a job when a worker fails to perform due to a technical error (e.g. web service connection interrupted).

### Scenario

1. Backend system is not accessible to worker
2. Worker responds failure back to orchestrator
3. Orchestrator retries a number of times
4. Orchestrator suspends workflow instance
5. Backend system becomes available
6. Orchestrator admin resumes workflow instance

### Step 1

Restart worker 1 in test mode.

```
# cd to worker1 folder
> dotnet run test
```

### Step 2

Start a new instance:

```
# cd to client folder
> dotnet run 1
```

### Step 3

Go to Operate and click on Incidents filter, click on the failed instance and expand the red incident information block. Read the incident information.

![bild](/img/Incident.png)

### Step 4

Resolve the issue by restarting the worker in normal mode.

```
# cd to worker1 folder
> dotnet run normal
```

### Step 5

Click on retry incident in Operate. Wait and watch how Operate changes to a running instance.

### Optional

You can try to redo this test but starting multiple instances (3) and the handle them as a batch in Operate.

## Exception handling in workflow

This scenario will test if the workflow can handle exception due to business errors. A business error is expected and handled in the workflow.

### Scenario

1. Backend system answer with an activation failure
2. Worker responds back to orchestrator with an exception
3. Workflow catches the exception and starts an exception flow
4. Workflow executes a task calling a Case Management System to compensate for the error
5. The compensation will result in an activation event sent from the backend system
6. Receive the activation event and then return back to the normal flow

### Step 1

Restart worker 2 in test mode.

```
# cd to worker2 folder
> dotnet run test
```

### Step 2

Start worker 6.

```
# cd to worker6 folder
> dotnet run
```

### Step 3

Start a new instance:

```
# cd to client folder
> dotnet run 2
```

### Step 4

Go to Operate and click on Running instances, open your workflow instance. Wait for the flow to reach the Receive ISK Activation Event.

![bild](/img/BusinessError.png)

### Step 5

Send the activation event and watch the exception flow return back to the normal flow in Operate.

```
# cd to system folder
> dotnet run 2
```

## Worker instance crashes

This scenario will test if the workflow can handle a worker crashing due to a runtime error.

### Scenario

1. A worker will pick up on a job but then crash
2. Workflow will time out and resend the job for another worker to pick up on

### Step 1

Restart worker 3 in test mode.

```
# cd to worker3 folder
> dotnet run test
```

### Step 2

Start a new instance:

```
# cd to client folder
> dotnet run 3
```

### Step 3

Wait for the worker to crash and the start another worker who can pick up the job after the workflow sends out a new attempt.

```
# cd to worker3 folder
> dotnet run normal
```

## Unexpected data error

This scenario will test if the workflow can handle an unexpected data error.

### Scenario

1. Backend system answer with a 400 Bad Request Error
2. Worker responds failure back to the workflow
3. Orchestrator suspends workflow instance
4. Orchestrator admin changes the data in the workflow instance
5. Orchestrator admin resumes workflow instance

### Step 1

Restart worker 4 in test mode.

```
# cd to worker4 folder
> dotnet run test
```

### Step 2

Start a new instance:

```
# cd to client folder
> dotnet run 4
```

### Step 3

Go to Operate and click on Incidents filter, click on the failed instance and expand the red incident information block. Read the incident information.

![bild](/img/Variable.png)

### Step 4

Resolve the issue by modifying the value of the 'source_account_id' variable. Change the length of the string from 36 characters to anything else.

### Step 5

Click on retry incident in Operate. Wait and watch how Operate changes to a running instance.

## Inaccessible backend service II

This scenario will test if the workflow can retry sending a job when a worker fails to perform due to a technical error (e.g. database connection interrupted). But with the difference from the first example - this worker will succeed on it's last attempt.

### Scenario

1. Backend system is not available to worker
2. Worker responds failure back to orchestrator
3. Orchestrator retries a number of times after which it succeedes

### Step 1

Restart worker 5 in test mode.

```
# cd to worker5 folder
> dotnet run test
```

### Step 2

Start a new instance:

```
# cd to client folder
> dotnet run 5
```

### Step 3

Watch the console output from worker 5:

```
Handling job: Key: 2251799813686978, Type: create-monthly-saving, WorkflowInstanceKey: 2251799813686937, BpmnProcessId: create-isk-process, WorkflowDefinitionVersion: 2, WorkflowKey: 2251799813685697, ElementId: create-monthly-saving, ElementInstanceKey: 2251799813686976, Worker: WorkerName, Retries: 3, Deadline: 2020-07-05 22:38:51, Variables: {"funds":[{"fund_id":"ABC Emerging Marketsfond","allocation":50},{"fund_id":"ABC Asienfond ex-Japan","allocation":25},{"fund_id":"ABC Sverige Småbolag","allocation":25}],"customer_id":"13","isk_activated":true,"isk_account_id":"60d6600b-3949-4006-a194-dc15a1fa612e","monthly_saving":500,"funds_allocated":true,"source_account_id":"87fd78b4-de0e-4899-ba02-a1aee2f5a47b"}, CustomHeaders: {}
Worker 5 failing with message: Backend system not available
Handling job: Key: 2251799813686978, Type: create-monthly-saving, WorkflowInstanceKey: 2251799813686937, BpmnProcessId: create-isk-process, WorkflowDefinitionVersion: 2, WorkflowKey: 2251799813685697, ElementId: create-monthly-saving, ElementInstanceKey: 2251799813686976, Worker: WorkerName, Retries: 2, Deadline: 2020-07-05 22:38:54, Variables: {"funds":[{"fund_id":"ABC Emerging Marketsfond","allocation":50},{"fund_id":"ABC Asienfond ex-Japan","allocation":25},{"fund_id":"ABC Sverige Småbolag","allocation":25}],"customer_id":"13","isk_activated":true,"isk_account_id":"60d6600b-3949-4006-a194-dc15a1fa612e","monthly_saving":500,"funds_allocated":true,"source_account_id":"87fd78b4-de0e-4899-ba02-a1aee2f5a47b"}, CustomHeaders: {}
Worker 5 failing with message: Backend system not available
Handling job: Key: 2251799813686978, Type: create-monthly-saving, WorkflowInstanceKey: 2251799813686937, BpmnProcessId: create-isk-process, WorkflowDefinitionVersion: 2, WorkflowKey: 2251799813685697, ElementId: create-monthly-saving, ElementInstanceKey: 2251799813686976, Worker: WorkerName, Retries: 1, Deadline: 2020-07-05 22:38:57, Variables: {"funds":[{"fund_id":"ABC Emerging Marketsfond","allocation":50},{"fund_id":"ABC Asienfond ex-Japan","allocation":25},{"fund_id":"ABC Sverige Småbolag","allocation":25}],"customer_id":"13","isk_activated":true,"isk_account_id":"60d6600b-3949-4006-a194-dc15a1fa612e","monthly_saving":500,"funds_allocated":true,"source_account_id":"87fd78b4-de0e-4899-ba02-a1aee2f5a47b"}, CustomHeaders: {}
Worker 5 completes job successfully.
```

Pay attention to the Retries property that goes from 3 to 1, after which it succeeds.

[back to toc](#table-of-content)

# Horizontal scaling

Use partitions to scale your workflow processing. Partitions are dynamically distributed in a Zeebe cluster and for each partition there is one leading broker at a time. This leader accepts requests and performs event processing for the partition. Let us assume you want to distribute workflow processing load over three machines. You can achieve that by bootstraping two partitions. But let's start with a single partion.

## One partition and one worker instance

### Step 1

Change directory to cluster-prom-grafana:
`> cd zeebe/cluster-prom-grafana`

Make sure the .env file contains these settings:

```
ZEEBE_DEMO_VERSION=0.20.0
ZEEBE_PARTITIONS_COUNT=1
```

### Step 2

Start brokers and gateway:  
`> docker-compose up -d zeebe-0 zeebe-1 zeebe-2 gateway`

Check Zeebe cluster status:
`> docker-compose exec gateway zbctl status`

The result will look similar to the following:

```
Cluster size: 3
Partitions count: 1
Replication factor: 3
Brokers:
  Broker 0 - 172.31.0.2:26501
    Partition 1 : Leader
  Broker 1 - 172.31.0.3:26501
    Partition 1 : Follower
  Broker 2 - 172.31.0.4:26501
    Partition 1 : Follower
```

### Step 3

Deploy a workflow definition for testing horizontal scaling.

Windows:

```
# cd to zeebe folder
> .\cli\zbctl.exe --insecure deploy ..\resources\create-isk-process-pt.bpmn
```

MacOS:

```
# cd to zeebe folder
$ ./cli/zbctl.darwin --insecure deploy ../resources/create-isk-process-pt.bpmn
```

### Step 4

Open a new command prompt and change directory `workers` folder.

### Step 5

Open the .env file and set your clone location.

```
ZEEBE_WORKER_LOCATION=<local-path>\zeebe-demo
```

Note: Mac users have to change `\` to `/` paths in docker-compose.yml

Build workers as Docker images:
`> docker-compose build`

Note: Building part will take a while due to inappropriate caching executing `dotnet restore`.

Run the workers as Docker containers using docker-compose:
`> docker-compose up -d`

### Step 6

Start Prometheus and Grafana:

```
# cd to zeebe/cluster-prom-grafana
> docker-compose up -d prometheus grafana
```

Open Grafana:

- Go to `localhost:3000`
- Login with `admin:pass`
- Open dashboard `Zeebe`
- Set auto refresh to every 5 second

After you started the simulation, you can see if workflow instances are being created and completed in the dashboard. Wait for a minute, if you don't see anything immediately.

### Step 7

Simulate work load by adding 30 request with 1000 ms delay:

```
# cd to simulate folder
> dotnet run 30 1000
```

## Two partitions and one worker instance

### Step 1

Make sure the .env file contains these settings:

```
ZEEBE_DEMO_VERSION=0.20.0
ZEEBE_PARTITIONS_COUNT=2
```

### Step 2

Recreating brokers and gateway:  
`> docker-compose up -d zeebe-0 zeebe-1 zeebe-2 gateway`

Check Zeebe cluster status:  
`> docker-compose exec gateway zbctl status`

The result will look similar to the following:

```
Cluster size: 3
Partitions count: 2
Replication factor: 3
Brokers:
  Broker 0 - 172.31.0.2:26501
    Partition 1 : Leader
    Partition 2 : Follower
  Broker 1 - 172.31.0.3:26501
    Partition 1 : Follower
    Partition 2 : Follower
  Broker 2 - 172.31.0.4:26501
    Partition 1 : Follower
    Partition 2 : Leader
```

### Step 3

Deploy the workflow definition again for testing horizontal scaling.

Windows:

```
# cd to zeebe folder
> .\cli\zbctl.exe --insecure deploy ..\resources\create-isk-process-pt.bpmn
```

MacOS:

```
# cd to zeebe folder
$ ./cli/zbctl.darwin --insecure deploy ../resources/create-isk-process-pt.bpmn
```

### Step 4

Simulate work load by adding 30 request with 1000 ms delay:

```
# cd to simulate folder
> dotnet run 30 1000
```

## Two partitions and two worker instance

### Step 1

Take down worker containers:
`> docker-compose down -v`

### Step 3

Start worker using scale command:  
`> docker-compose scale worker1=2 worker2=2 worker3=2 worker4=2 worker5=2`

### Step 4

Simulate work load by adding 30 request with 1000 ms delay:

```
# cd to simulate folder
> dotnet run 30 1000
```

### Step 5

Clean everything by `docker-compose down -v` for both (zeebe+prometheus+grafana and workers) compositions running containers.

## Conclusion for horizontal scaling

The effects on a single machine is not that obvious. But by looking at the image below showing Grafana over a 15 minutes period - there are some indications that the second partition resulted in more workflow instance in a shorter time frame and adding more workers increased the workflow throughput.

![bild](/img/HorizontalScaling.png)

[back to toc](#table-of-content)

# Fault tolerance

Zeebe allows a user to configure a replication factor when creating a topic. The replication factor determines how many “hot standby” copies of a partition are stored on other brokers. If one broker goes down, another can replace it with no data loss.

Because data is distributed across brokers in a cluster, Zeebe provides fault tolerance and high availability without the need for an external database, storing data directly on the filesystems in the servers where it’s deployed. Zeebe also does not require an external cluster orchestrator such as ZooKeeper. Zeebe is completely self-contained and self-sufficient.

## Step 1

Change directory to cluster-prom-grafana:
`> cd zeebe/cluster-prom-grafana`

Make sure the .env file contains these settings:

```
ZEEBE_DEMO_VERSION=0.20.0
ZEEBE_PARTITIONS_COUNT=2
```

## Step 2

Start brokers and gateway  
`> docker-compose up -d zeebe-0 zeebe-1 zeebe-2 gateway`

Check Zeebe cluster status:  
`> docker-compose exec gateway zbctl status`

The result will look similar to the following:

```
Cluster size: 3
Partitions count: 2
Replication factor: 3
Brokers:
  Broker 0 - 172.31.0.2:26501
    Partition 1 : Leader
    Partition 2 : Follower
  Broker 1 - 172.31.0.3:26501
    Partition 1 : Follower
    Partition 2 : Follower
  Broker 2 - 172.31.0.4:26501
    Partition 1 : Follower
    Partition 2 : Leader
```

## Step 3

Deploy a workflow definition for testing horizontal scaling.

Windows:

```
# cd to zeebe folder
> .\cli\zbctl.exe --insecure deploy ..\resources\create-isk-process-pt.bpmn
```

MacOS:

```
# cd to zeebe folder
$ ./cli/zbctl.darwin --insecure deploy ../resources/create-isk-process-pt.bpmn
```

## Step 4

Open a new command prompt and change directory `workers` folder.

## Step 5

Open the .env file and set your clone location.

```
ZEEBE_WORKER_LOCATION=<local-path>\zeebe-demo
```

Build workers as Docker images:
`> docker-compose build`

Note: This may take a while to build.

Run the workers as Docker containers using docker-compose:
`> docker-compose up -d`

## Step 6

Start Prometheus and Grafana:

```
# cd to zeebe/cluster-prom-grafana
> docker-compose up -d prometheus grafana
```

Open Grafana:

- Go to `localhost:3000`
- Login with `admin:pass`
- Open dashboard `Zeebe`
- Set auto refresh to every 5 second

After you started the simulation, you can see if workflow instances are being created and completed in the dashboard. Wait for a minute, if you don't see anything immediately.

## Step 7

Simulate work load by adding 500 request with 1000 ms delay:

```
# cd to simulate folder
> dotnet run 500 1000
```

## Step 8

Stop a broker who is a leader of at least one partition - check from Step 2 above:
`> docker-compose stop zeebe-2`

## Step 9

Check that Grafana is dropping the number of running instances until the election for a new leader has been completed.

![bild](/img/FaultTolerance.png)

## Step 10

Check Zeebe cluster status:  
`> docker-compose exec gateway zbctl status`

The result should be something like this:

```
Cluster size: 3
Partitions count: 2
Replication factor: 3
Brokers:
  Broker 0 - 172.18.0.2:26501
    Partition 1 : Follower
    Partition 2 : Leader
  Broker 1 - 172.18.0.5:26501
    Partition 1 : Leader
    Partition 2 : Follower
```

## Step 11

Restart the stopped broker by:
`> docker-compose start zeebe-2`

Check Zeebe cluster status:  
`> docker-compose exec gateway zbctl status`

The result should be something like this:

```
Cluster size: 3
Partitions count: 2
Replication factor: 3
Brokers:
  Broker 0 - 172.18.0.2:26501
    Partition 1 : Follower
    Partition 2 : Leader
  Broker 1 - 172.18.0.5:26501
    Partition 1 : Leader
    Partition 2 : Follower
  Broker 2 - 172.18.0.6:26501
    Partition 1 : Follower
    Partition 2 : Follower
```

## Step 12

Clean everything by `docker-compose down -v` for both (zeebe+prometheus+grafana and workers) compositions running containers.

[back to toc](#table-of-content)

# Workflow monitoring

It's possible to combine Kibana with Zeebe's Elasticsearch exporter to create a dashboard for monitoring business metrics, like workflow instance/job duration by workflow or data variable etc.

## Start Zeebe and Kibana

Start the containers in the background:

```
# cd to zeebe/operate-simple-monitor
> docker-compose up -d
```

## Start workers

Open a new command prompt and change directory `workers` folder.

Open the .env file and set your clone location.

```
ZEEBE_WORKER_LOCATION=<local-path>\zeebe-demo
```

Build workers as Docker images:
`> docker-compose build`

Note: This may take a while to build.

Run the workers as Docker containers using docker-compose:
`> docker-compose up -d`

## Deploy a workflow

Windows:

```
# cd to zeebe folder
> .\cli\zbctl.exe --insecure deploy ..\resources\create-isk-process-pt.bpmn
```

MacOS:

```
# cd to zeebe folder
$ ./cli/zbctl.darwin --insecure deploy ../resources/create-isk-process-pt.bpmn
```

## Simulate work load

Simulate work load by adding 30 request with 1000 ms delay:

```
# cd to simulate folder
> dotnet run 30 1000
```

## Kibana

Point you browser to Kibana at [http://localhost:5601](http://localhost:5601/).

### Create index patterns

- Click on **Management** section
- Click on **Index Patterns** part
- Click on **Create index pattern** button
- Type in `zeebe-record_variable*`
- Select `timestamp` in the **Time Filter field name** dropdown
- Click on **Create index** button

### Create a Scripted field

- Click on the **Scripted fields** tab
- Click **Add scripted field** button
- Select `painless` as **Language**, `number` as **Type** and **Script** should contain:

```
if (doc['value.name'].value == 'monthly_saving')
  return Integer.parseInt(doc['value.value'].value);
else
  return 0;
```

- Click on **Create field** button

### Create a Saved Search

- Click on **Discover** section
- Select `monthly_saving` field
- Click **Add a filter**
- Select `value.name` `is` `monthly_saving`
- Click **Save** and choose name `monthly_saving`

### Create a Visualization

- Click on **Visualize** section
- Click on **Create a visualization** button
- Select **Metric**
- Select the `monthly_saving` saved search
- Expand **Metric Count**
- Change Aggregation to `Sum`
- Select `monthly_saving' field
- Add Custom Label `Monthly Saving Invested`
- Click on the **blue play** button
- Click **Save** and choose name `monthly_saving`

### Create a Dashboard

- Click on **Dashoboard** section
- Click on **Create a new dashboard** button
- Click on **Add** button
- Select the `monthly_saving` visualization and remove the panel
- Click **Save** and choose name `ISK Dashboard`

### Monitor the workflow

- Change **Refresh interval** to `5 seconds`
- Change **Time range** to `Last hour`
- Simulate some more workload and keep an eye on the Dashboard

```
# cd to simulate folder
> dotnet run 30 1000
```

![bild](/img/Kibana.png)

[back to toc](#table-of-content)

# Workflow modelling

[Zeebe Modeler](https://docs.zeebe.io/getting-started/create-a-workflow.html) is a desktop modeling tool that allows you to build and configure workflow models using BPMN 2.0.

![bild](/img/create-isk-process.png)

Download the [modeler](https://github.com/zeebe-io/zeebe-modeler/releases) and unzip it to your local drive.

[back to toc](#table-of-content)

# Workflow debugging

Zeebe Simple Monitor is a Spring Boot application that can connect to Zeebe and registers for all events handled on the broker. It projects them into some own JPA entities locally in order to be displayed in a small HTML5 web application.

## Start Zeebe and Simple Monitor

Start the containers in the background:

```
# cd to zeebe/operate-simple-monitor
> docker-compose up -d
```

## Open Simple Monitor

Point your browser to [Simple Monitor](http://localhost:8082).

## Deploy the workflow

- Click on **Workflows** tab.
- Click on **New Deployment** button and choose the create-isk-process.bpmn file in the resources folder.
- Click **Deploy**.

## Debug a workflow instance

- Click on **Workflows** tab.
- Click on the deployed **workflow key**
- Click on the **New Instance** button

Set the following variables:

```
customer_id = "1"
monthly_saving = 500
source_account_id = "87fd78b4-de0e-4899-ba02-a1aee2f5a47b"
funds = [{"fund_id":"ABC Emerging Marketsfond","allocation":50},{"fund_id":"ABC Asienfond ex-Japan","allocation":25},{"fund_id":"ABC Sverige Småbolag","allocation":25}]
```

- Click on **Instances** tab
- Click on the **running instance**
- Click on the **Jobs** tab
- Click on **Complete** for the create-isk-account job type

Set the following variables:

```
isk_account_id = "10bc2c12-2b07-4856-b467-8763c3bdb872"
```

- **Refresh** the page
- Click on **Jobs** tab
- Click on **Throw Error** button for the activate-isk-account job type

```
Error Code = Activation Fault
```

- **Refresh** the page
- Click on **Jobs** tab
- Click on **Complete** for the activate-isk-account-manually job type

Set the following variables:

```
case_id = "e38a6840-9baa-457e-ae28-53745234560a"
```

- **Refresh** the page
- Click on **Message Subscriptions** tab
- Click on **Publish Message** button

Create a new inbound message:

```
Message Name = receive-isk-activation-event-message
Correlation Key = 1
```

- Click on **Publish Message**
- **Refresh** the page
- Click on **Jobs** tab
- Click on **Complete** for recurring_transfer job type

Set the following variables:

```
reccuring_transfer_done = true
```

- **Refresh** the page
- Click on **Jobs** tab
- Click on **Complete** for the create-fund-allocation job type

Set the following variables:

```
create-fund-allocation_done = true
```

- **Refresh** the page
- Click on **Jobs** tab
- Click on **Complete** for the create-monthly-saving job type

Set the following variables:

```
create-monthly-saving_done = true
```

- **Refresh** the page

The workflow instance is now completed.

![bild](/img/SimpleMonitor.png)

[back to toc](#table-of-content)

# Develop a worker

Applications that leverage the Zeebe broker need to be written using a client library that implements the Zeebe gRPC API.

Zeebe client libraries are available for:

- [C#](#create-a-worker-in-cs)
- [Go](#create-a-worker-in-go)
- [Java](#create-a-worker-in-java)
- JavaScript
- Python
- Ruby
- Rust

## Create a worker in CS

### Prerequisites

- [.NET Core SDK 2.1](https://dotnet.microsoft.com/download)

### Set up

- Create a folder `mkdir myworker`
- Change directory `cd myworker`
- Run `dotnet new "Console Application"`
- Run `dotnet add package zb-client --version 0.16.1`

### Bootstrapping

In C# code, instantiate the client as follows in Program.cs:

```
using System;
using Zeebe.Client;

namespace myworker
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = ZeebeClient.Builder()
                .UseGatewayAddress("127.0.0.1:26500")
                .UsePlainText()
                .Build();

            Console.WriteLine("Connected to Zeebe Broker.");

            // code goes here

            client.Close();

            Console.WriteLine("Disconnected from Zeebe Broker.");
        }
    }
}
```

### Add a job handler

```
using System;
using System.Threading;
using Zeebe.Client;
using Zeebe.Client.Api.Worker;
using Zeebe.Client.Api.Responses;

namespace myworker
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = ZeebeClient.Builder()
                .UseGatewayAddress("127.0.0.1:26500")
                .UsePlainText()
                .Build();

            Console.WriteLine("Connected to Zeebe Broker.");

            // open job worker
            using (var signal = new EventWaitHandle(false, EventResetMode.AutoReset))
            {
                client.NewWorker()
                      .JobType("create-isk-account")
                      .Handler(HandleJob)
                      .MaxJobsActive(5)
                      .Name(Environment.MachineName)
                      .PollInterval(TimeSpan.FromSeconds(1))
                      .Timeout(TimeSpan.FromMinutes(1))
                      .Open();

                Console.WriteLine("Worker is subscribing for jobs.");

                // blocks main thread, so that worker can run
                signal.WaitOne();
            }
        }

        private static void HandleJob(IJobClient jobClient, IJob job)
        {
            Console.WriteLine("Worker is handling a job: " + job);

            // worker business logic goes here
            // ...

            jobClient.NewCompleteJobCommand(job.Key)
                .Send()
                .GetAwaiter()
                .GetResult();
        }
    }
}
```

Read more about the [C# API]().

### Try it out

Follow the instructions in [Getting started](#getting-started) but without running worker1 as it will be replaced by your new worker.

Run **myworker** that you have developed.

```
> dotnet run
Connected to Zeebe Broker.
Worker is subscribing for jobs.
Worker is handling a job:
  Key: 2251799813685465,
  Type: create-isk-account,
  WorkflowInstanceKey: 2251799813685456,
  BpmnProcessId: create-isk-process,
  WorkflowDefinitionVersion: 1, WorkflowKey: 2251799813685249,
  ElementId: create-isk-account,
  ElementInstanceKey: 2251799813685464,
  Worker: WorkerName,
  Retries: 3,
  Deadline: 2020-07-12 10:29:55,
  Variables:
  {
    "funds": [
        {
            "fund_id": "ABC Emerging Marketsfond",
            "allocation": 50
        },
        {
            "fund_id": "ABC Asienfond ex-Japan",
            "allocation": 25
        },
        {
            "fund_id": "ABC Sverige Småbolag",
            "allocation": 25
        }
    ],
    "customer_id": "1",
    "monthly_saving": 500,
    "source_account_id": "87fd78b4-de0e-4899-ba02-a1aee2f5a47b"
  },
  CustomHeaders: {}
```

## Create a worker in Go

### Prerequisites

- [Go](https://golang.org/dl/)

### Set up

- Create a folder `mkdir myworker`
- Change directory `cd myworker`
- Run `go get -u github.com/zeebe-io/zeebe/clients/go/...`
- Create an empty file `myworker.go`

### Bootstrapping

In Go code, instantiate the client as follows in myworker.go:

```
package main

import (
	"log"

	"github.com/zeebe-io/zeebe/clients/go/pkg/zbc"
)

const BrokerAddr = "0.0.0.0:26500"

func main() {

	zbClient, err := zbc.NewClient(&zbc.ClientConfig{
		GatewayAddress:         BrokerAddr,
		UsePlaintextConnection: true,
	})

	if err != nil {
		panic(err)
	}

	log.Println("Connected to Zeebe Broker.")

	zbClient.Close()

	log.Println("Disconnected from Zeebe Broker.")
}
```

### Add a job handler

```
package main

import (
	"context"
	"log"

	"github.com/zeebe-io/zeebe/clients/go/pkg/entities"
	"github.com/zeebe-io/zeebe/clients/go/pkg/worker"
	"github.com/zeebe-io/zeebe/clients/go/pkg/zbc"
)

const BrokerAddr = "0.0.0.0:26500"

func main() {

	zbClient, err := zbc.NewClient(&zbc.ClientConfig{
		GatewayAddress:         BrokerAddr,
		UsePlaintextConnection: true,
	})

	if err != nil {
		panic(err)
	}

	log.Println("Connected to Zeebe Broker.")

	jobWorker := zbClient.NewJobWorker().JobType("create-isk-account").Handler(handleJob).Open()
	defer jobWorker.Close()

	log.Println("Worker is subscribing for jobs.")

	jobWorker.AwaitClose()

	zbClient.Close()

	log.Println("Disconnected from Zeebe Broker.")
}

func handleJob(client worker.JobClient, job entities.Job) {
	jobKey := job.GetKey()

	variables, err := job.GetVariablesAsMap()
	if err != nil {
		// failed to handle job as we require the variables
		failJob(client, job)
		return
	}

	variables["isk_account_id"] = "04ebfce6-df16-4a90-b50c-e7597eef0b81"
	request, err := client.NewCompleteJobCommand().JobKey(jobKey).VariablesFromMap(variables)
	if err != nil {
		// failed to set the updated variables
		failJob(client, job)
		return
	}

	log.Println("Worker is handling a job", job)

	ctx := context.Background()
	request.Send(ctx)
}

func failJob(client worker.JobClient, job entities.Job) {
	log.Println("Failed to complete job", job.GetKey())
	ctx := context.Background()
	client.NewFailJobCommand().JobKey(job.GetKey()).Retries(job.Retries - 1).Send(ctx)
}
```

Read more about the [Go API](https://docs.zeebe.io/clients/go-client/index.html).

### Try it out

Follow the instructions in [Getting started](#getting-started) but without running worker1 as it will be replaced by your new worker.

Run **myworker** that you have developed.

Windows:

```
> go build muworker.go
> myworker.exe
Connected to Zeebe Broker.
Worker is subscribing for jobs.
Worker is handling a job:
  Key: 2251799813685465,
  Type: create-isk-account,
  WorkflowInstanceKey: 2251799813685456,
  BpmnProcessId: create-isk-process,
  WorkflowDefinitionVersion: 1, WorkflowKey: 2251799813685249,
  ElementId: create-isk-account,
  ElementInstanceKey: 2251799813685464,
  Worker: WorkerName,
  Retries: 3,
  Deadline: 2020-07-12 10:29:55,
  Variables:
  {
    "funds": [
        {
            "fund_id": "ABC Emerging Marketsfond",
            "allocation": 50
        },
        {
            "fund_id": "ABC Asienfond ex-Japan",
            "allocation": 25
        },
        {
            "fund_id": "ABC Sverige Småbolag",
            "allocation": 25
        }
    ],
    "customer_id": "1",
    "monthly_saving": 500,
    "source_account_id": "87fd78b4-de0e-4899-ba02-a1aee2f5a47b"
  },
  CustomHeaders: {}
```

MacOS:

```
> go run myworker.go
Connected to Zeebe Broker.
Worker is subscribing for jobs.
Handling a job:
  Key: 2251799813685465,
  Type: create-isk-account,
  WorkflowInstanceKey: 2251799813685456,
  BpmnProcessId: create-isk-process,
  WorkflowDefinitionVersion: 1, WorkflowKey: 2251799813685249,
  ElementId: create-isk-account,
  ElementInstanceKey: 2251799813685464,
  Worker: WorkerName,
  Retries: 3,
  Deadline: 2020-07-12 10:29:55,
  Variables:
  {
    "funds": [
        {
            "fund_id": "ABC Emerging Marketsfond",
            "allocation": 50
        },
        {
            "fund_id": "ABC Asienfond ex-Japan",
            "allocation": 25
        },
        {
            "fund_id": "ABC Sverige Småbolag",
            "allocation": 25
        }
    ],
    "customer_id": "1",
    "monthly_saving": 500,
    "source_account_id": "87fd78b4-de0e-4899-ba02-a1aee2f5a47b"
  },
  CustomHeaders: {}
```

## Create a worker in Java

### Prerequisites

- Java 8
- [Apache Maven](https://maven.apache.org/)

### Set up

- Run the Maven command:

Windows:

```
mvn archetype:generate \
    "-DgroupId=se.abc" \
    "-DartifactId=myworker" \
    "-DarchetypeArtifactId=maven-archetype-quickstart" \
    "-DinteractiveMode=false"
```

MacOS:

```
mvn archetype:generate \
    -DgroupId=se.abc \
    -DartifactId=myworker \
    -DarchetypeArtifactId=maven-archetype-quickstart \
    -DinteractiveMode=false
```

### Configure the pom.xml file

- Set the compiler version:

```
<properties>
  <maven.compiler.target>1.8</maven.compiler.target>
  <maven.compiler.source>1.8</maven.compiler.source>
</properties>
```

- Add the Zeebe client library as dependency:

```
<dependency>
  <groupId>io.zeebe</groupId>
  <artifactId>zeebe-client-java</artifactId>
  <version>0.23.1</version>
</dependency>
```

- Add the Maven Shade plugin:

```
<build>
  <plugins>
    <!-- Maven Shade Plugin -->
    <plugin>
      <groupId>org.apache.maven.plugins</groupId>
      <artifactId>maven-shade-plugin</artifactId>
      <version>2.3</version>
      <executions>
        <!-- Run shade goal on package phase -->
        <execution>
          <phase>package</phase>
          <goals>
            <goal>shade</goal>
          </goals>
          <configuration>
            <transformers>
              <!-- add Main-Class to manifest file -->
              <transformer implementation="org.apache.maven.plugins.shade.resource.ManifestResourceTransformer">
                <mainClass>se.abc.App</mainClass>
              </transformer>
            </transformers>
          </configuration>
        </execution>
      </executions>
    </plugin>
  </plugins>
</build>
```

### Bootstrapping

In Java code, instantiate the client as follows in App.java:

```
package se.abc;

import io.zeebe.client.ZeebeClient;

public class App
{
    public static void main(final String[] args)
    {
        final ZeebeClient client = ZeebeClient.newClientBuilder()
            // change the contact point if needed
            .brokerContactPoint("127.0.0.1:26500")
            .usePlaintext()
            .build();

        System.out.println("Connected to Zeebe Broker.");

        // ...

        client.close();

        System.out.println("Disconnected from Zeebe Broker.");
    }
}
```

### Add a job handler

```
package se.abc;

import io.zeebe.client.ZeebeClient;
import io.zeebe.client.ZeebeClientBuilder;
import io.zeebe.client.api.response.ActivatedJob;
import io.zeebe.client.api.worker.JobClient;
import io.zeebe.client.api.worker.JobHandler;
import io.zeebe.client.api.worker.JobWorker;
import java.time.Duration;
import java.util.Scanner;

public final class App {
    public static void main(final String[] args) {
        final String broker = "127.0.0.1:26500";

        final String jobType = "create-isk-account";

        final ZeebeClientBuilder builder = ZeebeClient.newClientBuilder().brokerContactPoint(broker).usePlaintext();

        try (final ZeebeClient client = builder.build()) {

            System.out.println("Connected to Zeebe Broker.");

            final JobWorker workerRegistration = client.newWorker().jobType(jobType).handler(new ExampleJobHandler())
                    .timeout(Duration.ofSeconds(10)).open();

            System.out.println("Worker is subscribing for jobs.");

            // call workerRegistration.close() to close it

            // run until System.in receives exit command
            waitUntilSystemInput("exit");
        }
    }

    private static void waitUntilSystemInput(final String exitCode) {
        try (final Scanner scanner = new Scanner(System.in)) {
            while (scanner.hasNextLine()) {
                final String nextLine = scanner.nextLine();
                if (nextLine.contains(exitCode)) {
                    return;
                }
            }
        }
    }

    private static class ExampleJobHandler implements JobHandler {
        @Override
        public void handle(final JobClient client, final ActivatedJob job) {
            // here: business logic that is executed with every job
            System.out.println(job);
            client.newCompleteCommand(job.getKey()).send().join();
        }
    }
}
```

Read more about the [Java API](https://docs.zeebe.io/clients/java-client/)

### Try it out

Follow the instructions in [Getting started](#getting-started) but without running worker1 as it will be replaced by your new worker.

Build your maven project `mvn package` and then run **myworker** that you have developed.

```
> java -jar target/myworker-1.0-SNAPSHOT.jar
Opening worker.
Job worker opened and receiving jobs.
Handling a job:
  Key: 2251799813685465,
  Type: create-isk-account,
  WorkflowInstanceKey: 2251799813685456,
  BpmnProcessId: create-isk-process,
  WorkflowDefinitionVersion: 1, WorkflowKey: 2251799813685249,
  ElementId: create-isk-account,
  ElementInstanceKey: 2251799813685464,
  Worker: WorkerName,
  Retries: 3,
  Deadline: 2020-07-12 10:29:55,
  Variables:
  {
    "funds": [
        {
            "fund_id": "ABC Emerging Marketsfond",
            "allocation": 50
        },
        {
            "fund_id": "ABC Asienfond ex-Japan",
            "allocation": 25
        },
        {
            "fund_id": "ABC Sverige Småbolag",
            "allocation": 25
        }
    ],
    "customer_id": "1",
    "monthly_saving": 500,
    "source_account_id": "87fd78b4-de0e-4899-ba02-a1aee2f5a47b"
  },
  CustomHeaders: {}
```

[back to toc](#table-of-content)

# Zeebe and Kafka

Kafka Connect is the ecosystem of connectors into or out of Kafka. There are lots of existing connectors, e.g. for databases, key-value stores or file systems. So for example you can read data from a RDMS and push it to Elasticsearch or flat files.

![bild](/img/Kafka.png)

## Kafka Connect Zeebe

The connector can do two things:

Send messages to a Kafka topic when a workflow instance reaches a specific activity. This is a source in Kafka Connect speak.

Consume messages from a Kafka topic and correlate them to a workflow. This is a Kafka Connect sink.

![bild](/img/ZeebeKafka.png)

## Connect Zeebe with a Kafka Connect

### Prerequisites

- [Kafka](https://kafka.apache.org/downloads) locally installed

### Set up

Windows:

- Configure [plugins for Kafka Connect](https://docs.confluent.io/current/connect/userguide.html#installing-plugins)
  - Create a folder for plugins
  - Set the folder path in the `connect-standalone.properties` file (i.e. `plugin.path=<plugin_folder_location>/jars`)
  - Copy the Zeebe Connector plugin (`kafka-connect-zeebe-1.0.0-SNAPSHOT-uber.jar`) from `kafka/windows` folder to the Kafka plugin location
- Copy the .properties files from `kafka` folder to KAFKA_HOME/config

MacOS:

- Configure [plugins for Kafka Connect](https://docs.confluent.io/current/connect/userguide.html#installing-plugins)
  - Create a folder for plugins
  - Set the folder path in the `connect-standalone.properties` file (i.e. `plugin.path=<plugin_folder_location>/jars`)
  - Copy the Zeebe Connector plugin (`kafka-connect-zeebe-1.0.0-SNAPSHOT-uber.jar`) from `kafka/macos` folder to the Kafka plugin location
- Copy the .properties files from `kafka` folder to KAFKA_HOME/config

### Steps

Windows:

- [Start Zeebe](#start-zeebe)
- [Deploy the workflow](#deploy-a-workflow) (NOTE: Use the file create-isk-process-kafka.bpmn)
- Start ZooKeeper `.\bin\windows\zookeeper-server-start.bat config\zookeeper.properties`
- Start Kafka `.\bin\windows\kafka-server-start.bat config\server.properties`
- Create a Kafka sink topic, if not created `.\bin\windows\kafka-topics.bat --create --zookeeper localhost:2181 --replication-factor 1 --partitions 1 --topic isk-activation-events`
- Create a Kafka source topic, if not created `.\bin\windows\kafka-topics.bat --create --zookeeper localhost:2181 --replication-factor 1 --partitions 1 --topic isk-activation-request-events`
- List all topics created `.\bin\windows\kafka-topics.bat --list --zookeeper localhost:2181`
- Start Kafka Connect `.\bin\windows\connect-standalone.bat config\connect-standalone.properties config\isk-zeebe-sink.properties config\isk-zeebe-source.properties`
- Start a Kafka Producer `.\bin\windows\kafka-console-producer.bat --broker-list localhost:9092 --topic isk-activation-events`
- Start a Kafka Consumer `.\bin\windows\kafka-console-consumer.bat --bootstrap-server localhost:9092 --topic isk-activation-request-events`
- [Run the workers](#run-the-workers) in normal mode except worker 2 which should be in test mode `dotnet run test`
- [Run the workflow](#run-a-workflow) `dotnet run 1`
- [Operate the workflow](#operate-a-workflow)
- Wait for exception path from worker 2 and send a Kafka message from the Kafka Producer:

```
{"correlationKey":"1","messageName":"receive-isk-activation-event-message"}
```

- [Stop Zeebe](#stop-zeebe)

MacOS:

- [Start Zeebe](#start-zeebe)
- [Deploy the workflow](#deploy-a-workflow) (NOTE: Use the file create-isk-process-kafka.bpmn)
- Start ZooKeeper `./bin/zookeeper-server-start.sh config/zookeeper.properties`
- Start Kafka `./bin/kafka-server-start.sh config/server.properties`
- Create a Kafka sink topic, if not created `./bin/kafka-topics.sh --create --zookeeper localhost:2181 --replication-factor 1 --partitions 1 --topic isk-activation-events`
- Create a Kafka source topic, if not created `./bin/kafka-topics.sh --create --zookeeper localhost:2181 --replication-factor 1 --partitions 1 --topic isk-activation-request-events`
- List all topics created `./bin/kafka-topics.sh --list --zookeeper localhost:2181`
- Start Kafka Connect `./bin/connect-standalone.sh config/connect-standalone.properties config/isk-zeebe-source.properties config/isk-zeebe-sink.properties`
- Start a Kafka Producer `./bin/kafka-console-producer.sh --broker-list localhost:9092 --topic isk-activation-events`
- Start a Kafka Consumer `./bin/kafka-console-consumer.sh --bootstrap-server localhost:9092 --topic isk-activation-request-events`
- [Run the workers](#run-the-workers) in normal mode except worker 2 which should be in test mode `dotnet run test`
- [Run the workflow](#run-a-workflow) `dotnet run 1`
- [Operate the workflow](#operate-a-workflow)
- Wait for exception path from worker 2 and send a Kafka message from the Kafka Producer:

```
{"correlationKey":"1","messageName":"receive-isk-activation-event-message"}
```

- [Stop Zeebe](#stop-zeebe)

[back to toc](#table-of-content)

# Zeebe and Kubernetes

Installing Zeebe in a K8 cluster.

## Prerequisites

In order to use Zeebe in a Kubernetes cluster you need to have the following tools installed in your local environment:

- `kubectl`: Kubernetes Control CLI tool: installed and connected to your cluster
- `helm`: Kubernetes Helm CLI tool

### Google Kubernetes Engine

Follow this set up if you're running it in GKE.

- Order a Sandbox from [MyIT](https://myit.sebank.se/dwp/rest/share/OJSXG33VOJRWKSLEHVJVER2BIE2VMMCHGBHFOV2BKFAVKVSSIJIFUWCGJY2FKMKDGUTHEZLTN52XEY3FKR4XAZJ5KNJEIJTUMVXGC3TUJFSD2MBQGAYDAMBQGAYDAMBQGAYDCJTDN5XHIZLYORKHS4DFHVBUCVCBJRHUOX2IJ5GUKJTQOJXXM2LEMVZFG33VOJRWKTTBNVST243SNU======).
- Create a [Kubernetes cluster](https://console.cloud.google.com/kubernetes/).

### Creating a GKE cluster

Use the configuration settings below then click `Create`. Make sure that egress firewall rules are disabled.

**Cluster basic**

- Fill in cluster name `zb-cluster`
- Select location type `Zonal`
- Select the `europe-west1-c` region

**default-pool**

- Set `Number of nodes` to `12`

**Networking**

- Select `private cluster`
- Fill in master IP range `172.16.1.0/28`
- Uncheck `Automatically create secondary ranges`
- Make sure that `Pod secondary CIDR range` uses `podnet` and `Services secondary CIDR range` using `servicenet`
- Check `Enable master authorized networks`
  - Click Add Authorized Network
  - Name `SEBRange1` and Network `129.178.182.96/27`
  - Click Add Authorized Network
  - Name `SEBRange2` and Network `129.178.182.64/27`
  - Click Add Authorized Network
  - Name `SEBRange3` and Network `129.178.88.64/27`

**Security**

- Check `Enable Shielded GKE Nodes`

### Local installation

Optionally use Kubernetes locally:

- `kind`: [Kubernetes KIND](https://github.com/kubernetes-sigs/kind)

## Using HELM Charts for Kubernetes

This repository host Zeebe HELM charts for Kubernetes, this charts can be accessed by adding the following HELM repo to your HELM setup:

```
> helm repo add zeebe https://helm.zeebe.io
> helm repo update
```

There are three main charts which are represented in the following image:

![bild](/img/Charts.png)

You can install these Helm Charts after you have created a cluster.

First you need to set the GKE cluster context and here are som useful CLI commands:

```
> gcloud config set project <YOUR GCP PROJECT ID>
> gcloud config set compute/zone <YOUR GCP CLUSTER ZONE>
> kubectl config get-contexts
> kubectl config view
> kubectl config unset contexts.<CONTEXT>
> gcloud container clusters get-credentials <YOUR CLUSTER NAME>
```

In Google Kubernetes Engine:

`> helm install <YOUR HELM RELEASE NAME> zeebe/zeebe-full -f zeebe/k8/zeebe-k8-values.yaml`

_Note: The RELEASE NAME has to be the same as the GKE cluster name._

![bild](/img/Kubernetes.png)

In Kubernetes KIND:

`> helm install <YOUR HELM RELEASE NAME> zeebe/zeebe-full -f zeebe/k8/zeebe-kind-values.yaml`

## Check cluster status

Check that your Zeebe is up and running.

You have to use port forwarding to access services in the cluster's virtual network:

`> kubectl port-forward svc/<YOUR HELM RELEASE NAME>-zeebe-gateway 26500:26500`

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

This is what you should expect as a result when running a GKE installation:

```
Cluster size: 3
Partitions count: 3
Replication factor: 3
Gateway version: 0.23.4
Brokers:
  Broker 0 - cluster-1-zeebe-0.cluster-1-zeebe.default.svc.cluster.local:26501
    Version: 0.23.4
    Partition 1 : Leader
    Partition 2 : Leader
    Partition 3 : Follower
  Broker 1 - cluster-1-zeebe-1.cluster-1-zeebe.default.svc.cluster.local:26501
    Version: 0.23.4
    Partition 1 : Follower
    Partition 2 : Follower
    Partition 3 : Follower
  Broker 2 - cluster-1-zeebe-2.cluster-1-zeebe.default.svc.cluster.local:26501
    Version: 0.23.4
    Partition 1 : Follower
    Partition 2 : Follower
    Partition 3 : Leader
```

## Deploy a workflow

Deploy a workflow to the cluster.

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

## Operate a workflow

1. Run `kubectl port-forward svc/<YOUR HELM RELEASE NAME>-nginx-ingress-controller 80:80`
2. Open the web interface http://localhost:80 (login: demo/demo)
3. Click on running instances tab and then select the deployed workflow definition from the drop down list.

## Running workers on-prem

To complete this tests [deploy](#deploy-a-workflow) a workflow, [run](#run-the-workers) the workers, [run](#run-a-workflow) the workflow and [Operate](#operate-zeebe-in-the-gke-cluster) the workflow.

## Running workers in the cloud

Not implemented yet.

## Operate Zeebe in the GKE cluster

Follow this guide if you would like to use direct access instead of forwarding port 80:

- Go to VPC Network and Firewall
- Click on new Firewall rule
- Name it `open-for-zeebe-operate-from-abc`
- Select your default network, ingress direction, allow as action, specified service account as target, this project as scope, compute engine as target service account, and IP ranges as filter
- Enter 129.178.182.64/27, 129.178.182.96/27, 129.178.88.64/27 as source filters
- Click on create
- Run `kubectl get service zb-cluster-nginx-ingress-controller`
- Enter the external ip in your browser and login to Zeebe Operate `demo/demo`

## Cleaning up

You can remove these charts by running:

`> helm delete <YOUR HELM RELEASE NAME> --no-hooks`

You can delete the KIND cluster by running:

`> kind delete cluster`

[back to toc](#table-of-content)

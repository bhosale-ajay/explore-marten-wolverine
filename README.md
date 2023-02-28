# Read Me

## External Libraries

* Marten - Event Store, Document DB
* Wolverine - Mediator, EventBus, Outbox

## Runtime Setup

* RabbmitMQ, Postgres, and Pgadmin with Docker
* LMS.Borrowing.API as dotnet project (tye)

## Steps

1. Add following two entries to /etc/hosts
   
   ```
   127.0.0.1 cin.lms.com
   127.0.0.1 mason.lms.com
   ```

2. Restore Tool and Package(s)

   ```
   dotnet tools restore
   dotnet restore
   ```

3. To run RabbitMQ

   ```
   docker run --rm -it --hostname my-rabbit -p 15672:15672 -p 5672:5672 rabbitmq:3-management
   ```

4. To run service and postgres

   ```
   dotnet tye run -- watch
   ```

5. To generate Marten code.
  
   * Comment `borrowing-context` service in tye.yaml
   * Run `dotnet tye run -- watch` just for db
   * And then
     ```
     cd src\LMS.Borrowing.API
     dotnet run -- codegen write
     ```
   * Transfer generated files to LMS.Marten.Generated

6. Import `api-testing.postman_collection.json` to postman for quick testing.
var builder = DistributedApplication.CreateBuilder(args);

var mongo = builder.AddMongoDB("mongodb", port: 27017)
    .WithDataVolume();

var tasksDatabase = mongo.AddDatabase("tasksdb");

builder.AddProject<Projects.Task_Api>("task-api")
    .WithReference(tasksDatabase)
    .WaitFor(mongo);

builder.Build().Run();
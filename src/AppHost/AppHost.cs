// Створюємо Aspire application builder — центральну точку конфігурації всіх ресурсів застосунку.
var builder = DistributedApplication.CreateBuilder(args);

// Додаємо MongoDB як ресурс Aspire та фіксуємо порт, доступний на хості.
var mongo = builder.AddMongoDB("mongodb", port: 27017)
    // Зберігаємо дані MongoDB у Docker volume між перезапусками контейнера.
    .WithDataVolume();

// Створюємо в MongoDB базу даних tasksdb і зберігаємо її як окреме посилання для сервісів.
var tasksDatabase = mongo.AddDatabase("tasksdb");

// Додаємо RabbitMQ як ресурс Aspire з іменем rabbitmq.
var rabbitMq = builder.AddRabbitMQ("rabbitmq")
    // Вмикаємо RabbitMQ Management UI для перегляду черг, exchange-ів і повідомлень у браузері.
    .WithManagementPlugin()
    // Зберігаємо стан RabbitMQ, зокрема черги й повідомлення, у Docker volume між перезапусками.
    .WithDataVolume();

// Додаємо PostgreSQL як ресурс Aspire.
var postgres = builder.AddPostgres("postgres")
    // Зберігаємо дані PostgreSQL у Docker volume між перезапусками.
    .WithDataVolume();

// Створюємо окрему базу даних для аналітичної проєкції задач.
var analyticsDatabase = postgres.AddDatabase("analyticsdb");

// Реєструємо проєкт Task.Api як сервіс, яким керуватиме Aspire.
builder.AddProject<Projects.Task_Api>("task-api")
    // Передаємо API налаштування підключення до бази tasksdb.
    .WithReference(tasksDatabase)
    // Передаємо API налаштування підключення до RabbitMQ.
    .WithReference(rabbitMq)
    // Не запускаємо API, доки MongoDB не буде готова приймати з'єднання.
    .WaitFor(mongo)
    // Не запускаємо API, доки RabbitMQ не буде готовий приймати з'єднання.
    .WaitFor(rabbitMq);

// Реєструємо фоновий сервіс, який оброблятиме події задач для аналітики.
builder.AddProject<Projects.Task_Analytics>("task-analytics")
    // Передаємо Worker-у параметри підключення до RabbitMQ.
    .WithReference(rabbitMq)
    // Передаємо Worker-у параметри підключення до PostgreSQL analyticsdb.
    .WithReference(analyticsDatabase)
    // Не запускаємо Worker, доки RabbitMQ не готовий.
    .WaitFor(rabbitMq)
    // Не запускаємо Worker, доки PostgreSQL не готовий.
    .WaitFor(postgres);

// Збираємо конфігурацію розподіленого застосунку та запускаємо Aspire.
builder.Build().Run();

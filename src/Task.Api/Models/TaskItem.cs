using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace Task.Api.Models;

public sealed class TaskItem
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    [BsonRepresentation(BsonType.String)]
    public TaskItemType Type { get; set; }

    [BsonRepresentation(BsonType.String)]
    public TaskItemStatus Status { get; set; }

    [BsonRepresentation(BsonType.String)]
    public TaskItemPriority Priority { get; set; }

    public string? AssigneeId { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? DeletedAt { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskItemType
{
    [JsonStringEnumMemberName("design")]
    Design,

    [JsonStringEnumMemberName("develop")]
    Develop,

    [JsonStringEnumMemberName("review")]
    Review,

    [JsonStringEnumMemberName("testing")]
    Testing
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskItemStatus
{
    [JsonStringEnumMemberName("to do")]
    ToDo,

    [JsonStringEnumMemberName("in progress")]
    InProgress,

    [JsonStringEnumMemberName("done")]
    Done
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskItemPriority
{
    [JsonStringEnumMemberName("low")]
    Low,

    [JsonStringEnumMemberName("normal")]
    Normal,

    [JsonStringEnumMemberName("high")]
    High
}   

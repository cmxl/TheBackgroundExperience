namespace TheBackgroundExperience.Domain.Common.Notifications;

public static class NotificationTopics
{
	public const string ExchangeName = "notifications";
	public const string QueueName = "sse-notifications";
	
	public static class Student
	{
		public const string Created = "student.created";
		public const string Updated = "student.updated";
		public const string Cached = "student.cached";
		public const string Deleted = "student.deleted";
	}
	
	public static class Routing
	{
		public const string AllStudents = "student.*";
		public const string AllNotifications = "#";
	}
}
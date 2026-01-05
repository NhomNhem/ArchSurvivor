using System.Threading.Tasks;

namespace Sisus.Init.Internal
{
	internal static class TaskExtensions
	{
		internal static async Task<object> GetResult(this Task task)
		{
			object result;
			do
			{
				await task;
				result = task.GetType().GetProperty(nameof(Task<object>.Result)).GetValue(task);
				task = result as Task;
			}
			while(task != null);

			return result;
		}
	}
}
﻿namespace Utility.Shared.Saga;

public class SagaResult<T>
{
    public SagaResult(bool isSuccess, T results)
    {
        this.IsSuccess = isSuccess;
        this.Results = results;
    }

    public bool IsSuccess { get; set; }

    public T Results { get; }
}

public class CompensationActivityModel<T>
{
    public string CompensationName { get; set; }
    public Func<SagaResult<T>, Task> Action { get; set; }

    public CompensationActivityModel(string compensationName, Func<SagaResult<T>, Task> action)
    {
        this.CompensationName = compensationName;
        this.Action = action;
    }
}

public interface ISagaActivity<T>
{
    string? ActivityName { get; }
    Func<Task<SagaResult<T>>> Action { get; }
    List<Func<SagaResult<T>, Task>> Compensations { get; }
}

public class SagaActivity<T> : ISagaActivity<T>
{
    public string? ActivityName { get; }

    public Func<Task<SagaResult<T>>> Action { get; }

    public List<CompensationActivityModel<T>> Compensations { get; set; } = new List<CompensationActivityModel<T>>();

    Func<Task<SagaResult<T>>> ISagaActivity<T>.Action => () => Action().ContinueWith(t => new SagaResult<T>(t.Result.IsSuccess, t.Result.Results));

    List<Func<SagaResult<T>, Task>> ISagaActivity<T>.Compensations => Compensations.Select(c => (Func<SagaResult<T>, Task>)(r => c.Action(new SagaResult<T>(r.IsSuccess, (T)r.Results)))).ToList();

    public SagaActivity(string activityName, Func<Task<SagaResult<T>>> action)
    {
        this.ActivityName = activityName;
        this.Action = action;
    }
}

public class ActivityResult<T>
{
    public ActivityResult(string activityName, SagaResult<T> sagaResult)
    {
        this.ActivityName = activityName;
        this.SagaResult = sagaResult;
    }

    public string ActivityName { get; }
    public SagaResult<T> SagaResult { get; }
}

public class SagaBuilder<T>
{
    private List<ISagaActivity<T>> Activities { get; } = new List<ISagaActivity<T>>();

    public SagaBuilder(string sagaName)
    {
        this.SagaName = sagaName;
    }

    public string? SagaName { get; }

    public List<ActivityResult<T>> ActivityResults { get; } = new List<ActivityResult<T>>();

    public SagaBuilder<T> Activity(string activityName, Func<Task<SagaResult<T>>> action)
    {
        Activities.Add(new SagaActivity<T>(activityName!, action));
        return this;
    }

    public SagaBuilder<T> CompensationActivity(string activityName, string compensationName, Func<SagaResult<T>, Task> compensation)
    {
        var activity = Activities.OfType<SagaActivity<T>>().FirstOrDefault(e => e.ActivityName == activityName);
        if (activity != null)
        {
            activity.Compensations.Add(new CompensationActivityModel<T>(compensationName, compensation));
        }
        return this;
    }

    public async Task ExecuteAsync()
    {
        foreach (var activity in Activities)
        {
            try
            {
                var result = await activity.Action();

                if (result is not null)
                    ActivityResults.Add(new ActivityResult<T>(activity.ActivityName!, result));

                if (activity.Compensations.Any())
                {
                    foreach (var compensation in activity.Compensations)
                    {
                        await compensation(result!);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Activity {activity.ActivityName} failed with error: {ex.Message}");
                // Handle exception, compensate activities, etc.
                throw;
            }
        }
    }
}
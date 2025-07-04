namespace GenericTableAPI.Models;

public class RetryPolicyOptions
{
    public int RetryCount { get; set; } = 3;
    public int WaitTimeMilliseconds { get; set; } = 200;
}
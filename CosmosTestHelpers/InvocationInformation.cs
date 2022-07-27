namespace CosmosTestHelpers
{
    public class InvocationInformation
    {
        public string MethodName { get; }

        public InvocationInformation(string methodName)
        {
            MethodName = methodName;
        }
    }
}
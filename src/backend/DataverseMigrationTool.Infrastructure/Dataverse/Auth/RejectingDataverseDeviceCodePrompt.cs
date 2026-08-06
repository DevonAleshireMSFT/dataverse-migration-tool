namespace DataverseMigrationTool.Infrastructure.Dataverse.Auth;

internal sealed class RejectingDataverseDeviceCodePrompt : IDataverseDeviceCodePrompt
{
    public ValueTask ShowAsync(DataverseDeviceCodePromptContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        throw new InvalidOperationException(
            "Interactive Dataverse authentication requires a trusted device-code prompt implementation. " +
            "The default provider refuses to log device codes or tokens.");
    }
}

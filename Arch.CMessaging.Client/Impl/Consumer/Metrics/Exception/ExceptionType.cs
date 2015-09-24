namespace cmessaging.consumer.exception
{
    public enum ExceptionType
    {
        OnPulling,
        OnConsuming,
        OnAck,
        OnMessageHandle,
        OnMetadataSync,
        OnMessageRead,
        OnChannelCreate,
    }
}

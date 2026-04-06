using System.ServiceModel;

namespace IIS.Api.Soap;

[ServiceContract(Namespace = "http://iis.local/soap/tasks", Name = "TaskSearch")]
public interface ITaskSearchSoap
{
    [OperationContract]
    Task<TaskSearchSoapResponse> SearchTasksAsync(string searchTerm);
}

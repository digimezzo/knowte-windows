using System.ServiceModel;

namespace Knowte.Common.Services.Command
{
    [ServiceContract()]
    public interface ICommandService
    {
        [OperationContract()]

        void NewNote();
        [OperationContract()]
        void OpenNote(string noteTitle);

        [OperationContract()]
        void Show();
    }
}

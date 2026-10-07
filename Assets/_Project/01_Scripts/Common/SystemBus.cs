using OzGameLab01.Common.Messaging;
using OzGameLab01.Common.Operations;
using UnityEngine;

namespace OzGameLab01.Common
{
    /// <summary>
    /// 전역 메시지 및 작업 서비스. Get/Register는 기존 Facade 조회의 호환 경로입니다.
    /// </summary>
    public static class SystemBus
    {
        private static readonly FacadeRegistry Facades = new();
        public static MessageBus Messages { get; private set; } = new(Debug.LogException);
        public static OperationRegistry Operations { get; private set; } = new();

        /// <summary>
        /// 타입 기준으로 Facade를 등록합니다. 다른 소유자의 중복 등록은 오류입니다.
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            Facades.Register(service);
        }

        public static void Unregister<T>() where T : class
        {
            Facades.Unregister<T>();
        }

        /// <summary>
        /// 등록된 Facade를 조회합니다. 등록된 적이 없으면 null을 반환합니다.
        /// </summary>
        public static T Get<T>() where T : class
        {
            return Facades.Get<T>();
        }

        public static void Unregister<T>(T owner) where T : class => Facades.Unregister(owner);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            Messages.Dispose();
            Messages = new MessageBus(Debug.LogException);
            Operations = new OperationRegistry();
            Facades.Clear();
        }
    }
}

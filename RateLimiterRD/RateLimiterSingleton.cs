using StackExchange.Redis;

namespace RateLimiterRD
{
    public interface IRateLimiterSingleton
    {
        Task<bool> LimitExceeded(string ipAddress);
    }

    public class RateLimiterSingleton : IRateLimiterSingleton, IDisposable
    {
		private IConnectionMultiplexer? connection;
        private string db_prefix = "r_d-"; //Префикс для наших ключей.
        private readonly int window = 60; // Наше временное окно.
        private readonly long max_requests = 10; //Максимальное количество запросов.

        public RateLimiterSingleton(IConnectionMultiplexer connection)
        {
			this.connection = connection;
			Database = connection.GetDatabase(10); //Свободная база на тестовом сервере.
		}

		void IDisposable.Dispose()
		{
			connection?.Dispose();
			Database = null;
			connection = null;
			GC.SuppressFinalize(this);
		}

		public bool IsConnected => connection != null;

        public IDatabase? Database { get; private set; }

        public bool IsDisposed => Database == null;

        //Не обязательно должен быть IP. Передавать можем любой идентификатор пользователя, сеанса или прочего, по которому будет работать наше ограничение.
        public async Task<bool> LimitExceeded(string ipAddress)
        {
            string key = $"{db_prefix}-{ipAddress}";

            //Проверим подключены ли мы к redis и подключение к базе.
            if (IsConnected && !IsDisposed)
            {
                //_ = await db.PingAsync(); //PING

                DateTime current_time = DateTime.Now; //Текущее время запроса.
                DateTime trim_time = current_time.AddSeconds(-1 * window); //Получаем границу до которой будет вне нашего временного окна.

                //Удаляем элементы с временем вне нашего временного окна в отсортированном наборе:
                _ = await Database.SortedSetRemoveRangeByScoreAsync(key, 0, trim_time.Ticks); //Интерпритация ZREMRANGEBYSCORE библоотеки для работы с Redis.

                //Получаем количество элементов в отсортированном наборе:
                long request_count = await Database.SortedSetLengthAsync(key); //ZCARD

                //Проверяем превышение количества запросов 
                if (request_count < max_requests) {
                    //Добавляем в отсортированный набор:
                    _ = await Database.SortedSetAddAsync(key, current_time.Ticks, current_time.Ticks); //ZADD
                    _ = await Database.KeyExpireAsync(key, current_time.AddSeconds(window)); //EXPIRE Устанавливаем время жизни

                    return false; //Возвращаем что не превышен
                }
            }

			return true; // Во всех других случаях вернём что лимит превышен. При недоступности redis получается что вернём превышение.
                         // Возможно это меньшее зло чем исчерпаем ресурсы сервера службой без ограничителя. Вопрос для обсуждения.
                         // Как вариант возвращать не boolean а integer для разных вариантов. Например сможем обработать недоступность redis и вернуть 500ый.
        }
    }
}

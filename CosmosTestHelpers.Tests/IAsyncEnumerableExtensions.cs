namespace CosmosTestHelpers.Tests
{
    internal static class AsyncEnumerableExtensions
    {
        public static async Task<T> FirstAsync<T>(this IAsyncEnumerable<T> enumerable)
        {
            await foreach (var item in enumerable)
            {
                return item;
            }

            throw new InvalidOperationException("FirstAsync was called on a collection with zero elements");
        }      
        
        public static async Task<bool> AnyAsync<T>(this IAsyncEnumerable<T> enumerable)
        {
            await foreach (var unused in enumerable)
            {
                return true;
            }

            return false;
        }        
        
        public static async Task<T> FirstOrDefaultAsync<T>(this IAsyncEnumerable<T> enumerable)
        {
            await foreach (var item in enumerable)
            {
                return item;
            }

            return default(T);
        }
        
        public static async Task<T> SingleAsync<T>(this IAsyncEnumerable<T> enumerable)
        {
            var result = default(T);
            var found = false;
            await foreach (var item in enumerable)
            {
                if (found)
                {
                    throw new InvalidOperationException("SingleSync called on a collection with more than one element");
                }

                found = true;
                result = item;
            }

            if (!found)
            {
                
                throw new InvalidOperationException("SingleAsync was called on a collection with zero elements");
            }

            return result;
        }        
        
        public static async Task<T> SingleOrDefaultAsync<T>(this IAsyncEnumerable<T> enumerable)
        {
            var result = default(T);
            var found = false;
            await foreach (var item in enumerable)
            {
                if (found)
                {
                    throw new InvalidOperationException("SingleSync called on a collection with more than one element");
                }
                
                found = true;
                result = item;
            }

            return result;
        }
        
        public static async Task<IList<T>> ToListAsync<T>(this IAsyncEnumerable<T> enumerable)
        {
            var result = new List<T>();
            await foreach (var item in enumerable)
            {
                result.Add(item);
            }

            return result;
        }  
    }
}
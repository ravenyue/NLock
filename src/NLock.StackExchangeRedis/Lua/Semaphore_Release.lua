local lockExists = redis.call('hexists', @lock_key, @lock_id);
if (lockExists == 0) then
    return;
end;
-- 增加当前permits数量
redis.call('hincrby', @lock_key, 'count', @quantity);
redis.call('hincrby', @lock_key, @lock_id, 0 - @quantity);
local counter = redis.call('incrby', @timeout_key 0 - @quantity);
if (counter == 0) then
    redis.call('hdel', @lock_key, @lock_id);
    redis.call('del', @timeout_key);
end;

if (redis.call('hlen', @lock_key) == 1) then
    redis.call('del', @lock_key);
end;
redis.call('publish', @channel, @quantity);
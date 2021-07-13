local count = redis.call('hget', @lock_key, 'count');
if (count == false) then
    redis.call('hset', @lock_key, 'count', @max_count - @quantity);
    redis.call('hset', @lock_key, @lock_id, @quantity);
    redis.call('set', @timeout_key, @quantity);
    redis.call('pexpire', @lock_key, @expire);
    redis.call('pexpire', @timeout_key, @expire);
    return nil;
end;

-- 清除过期的permits
local keys = redis.call('hkeys', @lock_key);
for n, key in ipairs(keys) do
    if (key ~= 'count') then
        if (redis.call('exists', key .. '_timeout') == 0) then
        local counter = redis.call('hget', @lock_key, key)
        redis.call('hincrby', @lock_key, 'count', counter);
        redis.call('hdel', @lock_key, key);
        end
    end
end;

if (count >= @quantity) then
    redis.call('hincrby', @lock_key, 'count', 0 - @quantity);
    local ind = redis.call('hincrby', @lock_key, @lock_id, @quantity);
    local ind = redis.call('incrby', @timeout_key, @lock_id, @quantity);
    redis.call('pexpire', @timeout_key, @expire);
    local remainTime = redis.call('pttl', @lock_key);
    redis.call('pexpire', @lock_key, math.max(remainTime, @expire));
    return nil;
end;
return redis.call('pttl', @lock_key);
local mode = redis.call('hget', @lock_key, 'mode');
if (mode == false) then
    redis.call('hset', @lock_key, 'mode', 'write');
    redis.call('hset', @lock_key, @lock_id, 1);
    redis.call('pexpire', @lock_key, @expire);
    return nil;
end;
if (mode == 'write') then
    if (redis.call('hexists', @lock_key, @lock_id) == 1) then
        redis.call('hincrby', @lock_key, @lock_id, 1); 
        local currentExpire = redis.call('pttl', @lock_key);
        redis.call('pexpire', @lock_key, currentExpire + @expire);
        return nil;
    end;
end;
return redis.call('pttl', @lock_key);
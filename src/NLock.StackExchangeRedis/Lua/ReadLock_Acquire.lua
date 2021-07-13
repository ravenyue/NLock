local mode = redis.call('hget', @lock_key, 'mode');
if (mode == false) then
    redis.call('hset', @lock_key, 'mode', 'read');
    redis.call('hset', @lock_key, @lock_id, 1);
    redis.call('set', @timeout_key .. ':1', 1);
    redis.call('pexpire', @timeout_key .. ':1', @expire);
    redis.call('pexpire', @lock_key, @expire);
    return nil;
end;
if (mode == 'read') or (mode == 'write' and redis.call('hexists', @lock_key, @write_lock) == 1) then
    local ind = redis.call('hincrby', @lock_key, @lock_id, 1); 
    local key = @timeout_key .. ':' .. ind;
    redis.call('set', key, 1);
    redis.call('pexpire', key, @expire);
    local remainTime = redis.call('pttl', @lock_key);
    redis.call('pexpire', @lock_key, math.max(remainTime, @expire));
    return nil;
end;
return redis.call('pttl', @lock_key);

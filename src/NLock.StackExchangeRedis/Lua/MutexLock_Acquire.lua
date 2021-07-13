if (redis.call('exists', @lock_key) == 0) then
    redis.call('hset', @lock_key, @lock_id, 1);
    redis.call('pexpire', @lock_key, @expire);
    return nil;
end;
if (redis.call('hexists', @lock_key, @lock_id) == 1) then
    redis.call('hincrby', @lock_key, @lock_id, 1);
    redis.call('pexpire', @lock_key, @expire);
    return nil;
end;
return redis.call('pttl', @lock_key);

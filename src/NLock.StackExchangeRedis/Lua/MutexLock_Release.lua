if (redis.call('hexists', @lock_key, @lock_id) == 0) then
    return nil;
end;
local counter = redis.call('hincrby', @lock_key, @lock_id, -1);
if (counter > 0) then
    redis.call('pexpire', @lock_key, @expire);
    return 0;
else
    redis.call('del', @lock_key);
    redis.call('publish', @channel, @msg);
    return 1;
end;
return nil;

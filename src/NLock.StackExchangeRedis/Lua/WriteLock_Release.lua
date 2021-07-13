local mode = redis.call('hget', @lock_key, 'mode');
if (mode == false) then
    redis.call('publish', @channel, @read_unlock_msg);
    return 1;
end;
if (mode == 'write') then
    local lockExists = redis.call('hexists', @lock_key, @lock_id);
    if (lockExists == 0) then
        return nil;
    else
        local counter = redis.call('hincrby', @lock_key, @lock_id, -1);
        if (counter > 0) then
            redis.call('pexpire', @lock_key, @expire);
            return 0;
        else
            redis.call('hdel', @lock_key, @lock_id);
            if (redis.call('hlen', @lock_key) == 1) then
                redis.call('del', @lock_key);
                redis.call('publish', @channel, @read_unlock_msg);
            else
                -- has unlocked read-locks
                redis.call('hset', @lock_key, 'mode', 'read');
            end;
            return 1;
        end;
    end;
end;
return nil;

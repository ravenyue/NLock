local mode = redis.call('hget', @lock_key, 'mode');
if (mode == false) then
    redis.call('publish', @channel, @unlock_msg);
    return 1;
end;
local lockExists = redis.call('hexists', @lock_key, @lock_id);
if (lockExists == 0) then
    return nil;
end;

local counter = redis.call('hincrby', @lock_key, @lock_id, -1);
if (counter == 0) then
    redis.call('hdel', @lock_key, @lock_id);
end;
redis.call('del', @timeout_key .. ':' .. (counter+1));

if (redis.call('hlen', @lock_key) > 1) then
    local maxRemainTime = -3;
    local keys = redis.call('hkeys', @lock_key);
    for n, key in ipairs(keys) do
        counter = tonumber(redis.call('hget', @lock_key, key));
        if type(counter) == 'number' then
            for i=counter, 1, -1 do
                local remainTime = redis.call('pttl', @timeout_prefix .. ':' .. key .. ':rwlock_timeout:' .. i);
                maxRemainTime = math.max(remainTime, maxRemainTime);
            end;
        end;
    end;

    if maxRemainTime > 0 then
        redis.call('pexpire', @lock_key, maxRemainTime);
        return 0;
    end;

    if mode == 'write' then
        return 0;
    end;
end;

redis.call('del', @lock_key);
redis.call('publish', @channel, @unlock_msg);
return 1;
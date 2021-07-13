local counter = redis.call('hget', @lock_key, @lock_id);
if (counter ~= false) then
    redis.call('pexpire', @lock_key, @expire);

    if (redis.call('hlen', @lock_key) > 1) then
        local keys = redis.call('hkeys', @lock_key);
        for n, key in ipairs(keys) do
            counter = tonumber(redis.call('hget', @lock_key, key));
            if type(counter) == 'number' then
                for i=counter, 1, -1 do
                    redis.call('pexpire', @timeout_prefix .. ':' .. key .. ':rwlock_timeout:' .. i, @expire);
                end;
            end; 
        end;
    end;

    return 1;
end;
return 0;

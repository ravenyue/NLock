local counter = redis.call('hget', @lock_key, @lock_id);
if (counter ~= false) then
    redis.call('pexpire', @lock_key, @expire);
    redis.call('pexpire', @timeout_key, @expire);
    return 1;
end;
return 0;


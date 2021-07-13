if (redis.call('hexists', @lock_key, @lock_id) == 1) then
    redis.call('pexpire', @lock_key, @expire);
    return 1;
end;
return 0;
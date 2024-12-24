# Seer

This is a web application firewall that denies IPv4 addresses to your nginx server. It is a helper to make web application administration easy. It also has a packet inspection mechanism to block IP addresses based on keywords defined in your ruleset.

### How to setup?

- Read the `rules.sample` to learn how to make your own ruleset.
- Read the `.config.json.sample` to learn how to set it up. But, you will be prompted to create this config if you don't have one. However, if you have this file and it was not setup properly, the application might not work as expected.
- Ensure you are logging the nginx server. And, only log the source IP, not the destination IP.

### How to execute?

Download the file in the release section of this GitHub repo.

```dotnet
sudo dotnet run 
```
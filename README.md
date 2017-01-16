# Bitcoin Core #

##API##

Swagger available on /swagger/ui/index/index.html

###POST /api/transaction/transfer###

```
public class TransferRequest
    {
        public string SourceAddress { get; set; }

        public string DestinationAddress { get; set; }

        public decimal Amount { get; set; }

        public string Asset { get; set; }
    }
```

###POST /api/transaction/swap###
```
public class SwapRequest
    {
        public string MultisigCustomer1 { get; set; }

        public decimal Amount1 { get; set; }

        public string Asset1 { get; set; }

        public string MultisigCustomer2 { get; set; }

        public decimal Amount2 { get; set; }

        public string Asset2 { get; set; }
    }
```

###GET /api/wallet/<client_public_key>

Uses <client_public_key> as URL segment


## Job ##

### indata queue ###

Need to run some server jobs
Pass Data as JSON string

####Types####

- 1 - get bitcoins from WalletAddress, generate Count fee outputs with FeeAmount volume.

#####EXAMPLE#####
```
{"Type": 1, Data: "{\"WalletAddress\": \"xxxxx\", \"FeeAmount\": 0.001, \"Count\": 100}" }
```


### signed-transactions queue ###

Need to broadcast signed by client transactions

#####EXAMPLE#####
```
{"TransactionId": "<guid>", "Transaction": "<hex>"}
```
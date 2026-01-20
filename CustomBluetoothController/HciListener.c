#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <sys/socket.h>
#include <bluetooth/bluetooth.h>
#include <bluetooth/hci.h>
#include <bluetooth/hci_lib.h>

int main() {
    int dev_id = hci_get_route(NULL);
    int sock = hci_open_dev(dev_id);
    if (sock < 0) {
        perror("HCI open failed");
        return 1;
    }

    struct hci_filter old, newf;
    socklen_t olen = sizeof(old);

    getsockopt(sock, SOL_HCI, HCI_FILTER, &old, &olen);

    hci_filter_clear(&newf);
    hci_filter_all_events(&newf);
    hci_filter_all_ptypes(&newf);
    setsockopt(sock, SOL_HCI, HCI_FILTER, &newf, sizeof(newf));

    unsigned char buf[1024];
    while (1) {
        int len = read(sock, buf, sizeof(buf));
        if (len > 0) {
            printf("Read %d bytes:\n", len);
            for (int i = 0; i < len; i++) printf("%02x ", buf[i]);
            printf("\n");
        }
    }

    setsockopt(sock, SOL_HCI, HCI_FILTER, &old, sizeof(old));
    close(sock);
    return 0;
}

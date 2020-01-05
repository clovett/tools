#!/usr/bin/env python
import argparse
import sys
import logging
import socket
import requests
import json
import time
from collections import namedtuple
from threading import Event, Thread


logger = logging.getLogger('client')
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(message)s')
STOP = Event()

def publish_endpoint(name, local_addr):
    msg = {
        "Name":name,
        "Date":"2020-01-05T09:40:05.4159018Z",
        "LocalAddress": local_addr[0],
        "LocalPort": str(local_addr[1])
    }
    headers = {'content-type': 'application/json'}
    url = "http://lovettsoftware.com/p2pserver.aspx?type=add"
    data = json.dumps(msg)
    r = requests.post(url, data=data, headers=headers)
    if (r.status_code != 200):
        raise Exception(string.format("publish failed {}", r.status_code))
    d = json.loads(r.text)
    if 'error' in d:
        raise Exception(d['error'])
    return namedtuple("Client", d.keys())(*d.values())


def find_endpoint(name):
    msg = {
        "Name":name
    }
    headers = {'content-type': 'application/json'}
    url = "http://lovettsoftware.com/p2pserver.aspx?type=find"
    data = json.dumps(msg)
    r = requests.post(url, data=data, headers=headers)
    if (r.status_code != 200):
        raise Exception(string.format("find failed {}", r.status_code))
    d = json.loads(r.text)
    if 'error' in d:
        raise Exception(d['error'])
    return namedtuple("Client", d.keys())(*d.values())


def remove_endpoint(name):
    msg = {
        "Name":name
    }
    headers = {'content-type': 'application/json'}
    url = "http://lovettsoftware.com/p2pserver.aspx?type=delete"
    data = json.dumps(msg)
    r = requests.post(url, data=data, headers=headers)
    if (r.status_code != 200):
        raise Exception(string.format("remove failed {}", r.status_code))
    d = json.loads(r.text)
    if 'error' in d:
        raise Exception(d['error'])
    return namedtuple("Client", d.keys())(*d.values())


def accept(port):
    logger.info("accept %s", port)
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    #s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEPORT, 1)
    s.bind(('', port))
    s.listen(1)
    s.settimeout(5)
    while not STOP.is_set():
        try:
            conn, addr = s.accept()
        except socket.timeout as e:
            print(e)
            continue
        else:
            logger.info("Accept %s connected!", port)
            # STOP.set()


def connect(local_addr, addr):
    logger.info("connect from %s to %s", local_addr, addr)
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    #s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEPORT, 1)
    s.bind(local_addr)
    while not STOP.is_set():
        try:
            s.connect(addr)
        except socket.error as e:
            print(e)
            continue
        # except Exception as exc:
        #     logger.exception("unexpected exception encountered")
        #     break
        else:
            logger.info("connected from %s to %s success!", local_addr, addr)
            # STOP.set()


def main(local_name, remote_name):
    sa = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sa.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    sa.connect(('50.62.160.103', 80)) # ('host', port))
    priv_addr = sa.getsockname()

    local_data = publish_endpoint(local_name, priv_addr)
    pub_addr = (local_data.RemoteAddress, int(local_data.RemotePort))
    logger.info("client %s %s - received data: %s", priv_addr[0], priv_addr[1], local_data)

    remote_client = None
    while remote_client is None:
        try:
            remote_client = find_endpoint(remote_name)
        except:
            print('.', end='', flush=True)
            time.sleep(1)

    client_pub_addr = (remote_client.RemoteAddress, int(remote_client.RemotePort))
    client_priv_addr = (remote_client.LocalAddress, int(remote_client.LocalPort))
    logger.info(
        "client public is %s and private is %s, peer public is %s private is %s",
        pub_addr, priv_addr, client_pub_addr, client_priv_addr,
    )

    threads = {
        '0_accept': Thread(target=accept, args=(priv_addr[1],)),
        '1_accept': Thread(target=accept, args=(client_pub_addr[1],)),
        '2_connect': Thread(target=connect, args=(priv_addr, client_pub_addr,)),
        '3_connect': Thread(target=connect, args=(priv_addr, client_priv_addr,)),
    }
    for name in sorted(threads.keys()):
        logger.info('start thread %s', name)
        threads[name].start()

    input("press enter to terminate")
    STOP.set()

    print("Removing registration for : " + local_name)
    remove_endpoint(local_name)

    while threads:
        keys = list(threads.keys())
        for name in keys:
            try:
                threads[name].join(1)
            except TimeoutError:
                continue
            if not threads[name].is_alive():
                threads.pop(name)


if __name__ == '__main__':
    logging.basicConfig(level=logging.INFO, message='%(asctime)s %(message)s')
    parser = argparse.ArgumentParser(description="Create p2p client using relay service")
    parser.add_argument("local_name", help="Name of local service")
    parser.add_argument("remote_name", help="Name of remote service we want to connect to")
    args = parser.parse_args()
    main(args.local_name, args.remote_name)

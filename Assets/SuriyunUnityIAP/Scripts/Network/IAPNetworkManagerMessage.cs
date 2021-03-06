﻿using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace Suriyun.UnityIAP
{
    public class IAPNetworkManagerMessage
    {
        public enum ServerBuyProductFail
        {
            None,
            NoProduct,
            ValidateFail
        }
        public static System.Action<NetworkConnection, MsgBuyProductFromClient, BaseIAPProduct> onBuyProductSuccess;
        public static System.Action<NetworkConnection, MsgBuyProductFromClient, ServerBuyProductFail> onBuyProductFail;
        public static System.Action<List<BaseIAPProduct>> onClientProductsResponse;
        public static void OnServerBuyProduct<T>(NetworkMessage netMsg) where T : BaseIAPProduct
        {
            T iapProduct = null;
            ServerBuyProductFail fail = ServerBuyProductFail.None;
            MsgBuyProductFromClient msg = netMsg.ReadMessage<MsgBuyProductFromClient>();
            if (ValidateIAP(msg, out iapProduct, out fail))
            {
                if (onBuyProductSuccess != null)
                    onBuyProductSuccess(netMsg.conn, msg, iapProduct);
            }
            else
            {
                if (onBuyProductFail != null)
                    onBuyProductFail(netMsg.conn, msg, fail);
            }
        }

        public static bool ValidateIAP<T>(MsgBuyProductFromClient msg, out T iapProduct, out ServerBuyProductFail fail) where T : BaseIAPProduct
        {
            iapProduct = null;
            fail = ServerBuyProductFail.None;
            // Variables from message
            string productId = msg.productId;
            if (IAPManager<T>.Instance.ConsumableProducts.TryGetValue(productId, out iapProduct))
                return true;
            else
                fail = ServerBuyProductFail.NoProduct;
            return false;
        }

        public static void OnServerProductsRequest<T>(NetworkMessage netMsg) where T : BaseIAPProduct
        {
            // TODO: May receive type of list via message
            MsgRequestProductsFromClient msg = netMsg.ReadMessage<MsgRequestProductsFromClient>();
            List<T> iapProducts = new List<T>();
            var keyValueList = IAPManager<T>.Instance.ConsumableProducts.GetEnumerator();
            while (keyValueList.MoveNext())
                iapProducts.Add(keyValueList.Current.Value as T);

            IAPProducts<T> productsList = new IAPProducts<T>();
            productsList.iapProducts = iapProducts;

            MsgResponseProductsFromServer resMsg = new MsgResponseProductsFromServer();
            resMsg.jsonProducts = JsonUtility.ToJson(productsList);
            netMsg.conn.Send(MsgResponseProductsFromServer.MsgId, resMsg);
        }

        public static void OnClientProductsResponse<T>(NetworkMessage netMsg) where T : BaseIAPProduct
        {
            MsgResponseProductsFromServer msg = netMsg.ReadMessage<MsgResponseProductsFromServer>();
            IAPProducts<T> productsList = JsonUtility.FromJson<IAPProducts<T>>(msg.jsonProducts);
            if (onClientProductsResponse != null)
                onClientProductsResponse(productsList.ToBaseList());
        }

        [System.Serializable]
        public struct IAPProducts<T> where T : BaseIAPProduct
        {
            public List<T> iapProducts;

            public List<BaseIAPProduct> ToBaseList()
            {
                List<BaseIAPProduct> baseList = new List<BaseIAPProduct>();
                foreach (var product in iapProducts)
                {
                    baseList.Add(product);
                }
                return baseList;
            }
        }
    }
}

/*
 * MIT License
 *
 * Copyright (c) 2024 Satania
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using UnityEngine;

namespace net.satania_shopping.tabakosystem
{
    public static class TransformUtility
    {
        public static void MirrorX(Transform myTransform, Transform parent)
        {
            Vector3 localPos = parent.InverseTransformPoint(myTransform.position);
            localPos.x = -localPos.x;
            Vector3 pos = parent.TransformPoint(localPos);

            Quaternion localRot = Quaternion.Inverse(parent.rotation) * myTransform.rotation;
            Matrix4x4 localMatrix = Matrix4x4.TRS(Vector3.zero, localRot, Vector3.one);
            Matrix4x4 reflectionMatrix = Matrix4x4.Scale(new Vector3(-1, 1, 1));
            Matrix4x4 mirroredMatrix = reflectionMatrix * localMatrix * reflectionMatrix;
            Quaternion mirroredLocalRot = mirroredMatrix.rotation;

            mirroredLocalRot *= Quaternion.Euler(0f, 180f, 0f);

            Quaternion rot = parent.rotation * mirroredLocalRot;

            myTransform.position = pos;
            myTransform.rotation = rot;
        }
    }
}
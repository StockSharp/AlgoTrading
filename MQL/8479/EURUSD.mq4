#include <stdlib.mqh>
double feature2[47];
 
/* inarray[0] is I */
/* inarray[1] is I */
/* inarray[2] is I */
/* inarray[3] is I */
/* inarray[4] is I */
/* inarray[5] is I */
/* inarray[6] is I */
/* inarray[7] is I */
/* inarray[8] is I */
/* inarray[9] is I */
/* outarray[0] is A */

void EURUSDn(double & inarray[], double & outarray[]){ 

if (inarray[0]<-0.9968153) inarray[0] = -0.9968153;
if (inarray[0]>0.9912279) inarray[0] = 0.9912279;
inarray[0] = (inarray[0] + 0.9968153) / 1.988043;
 
if (inarray[1]<-0.9876356) inarray[1] = -0.9876356;
if (inarray[1]>0.9751329) inarray[1] = 0.9751329;
inarray[1] = (inarray[1] + 0.9876356) / 1.962769;
 
if (inarray[2]<-0.9455376) inarray[2] = -0.9455376;
if (inarray[2]>0.9703116) inarray[2] = 0.9703116;
inarray[2] = (inarray[2] + 0.9455376) / 1.915849;
 
if (inarray[3]<-0.8731447) inarray[3] = -0.8731447;
if (inarray[3]>0.9568092) inarray[3] = 0.9568092;
inarray[3] = (inarray[3] + 0.8731447) / 1.829954;
 
if (inarray[4]<-0.9228548) inarray[4] = -0.9228548;
if (inarray[4]>0.9776543) inarray[4] = 0.9776543;
inarray[4] = (inarray[4] + 0.9228548) / 1.900509;
 
if (inarray[5]<-0.8458624) inarray[5] = -0.8458624;
if (inarray[5]>0.9684225) inarray[5] = 0.9684225;
inarray[5] = (inarray[5] + 0.8458624) / 1.814285;
 
if (inarray[6]<-0.8250612) inarray[6] = -0.8250612;
if (inarray[6]>0.9103971) inarray[6] = 0.9103971;
inarray[6] = (inarray[6] + 0.8250612) / 1.735458;
 
if (inarray[7]<-0.8170462) inarray[7] = -0.8170462;
if (inarray[7]>0.7589043) inarray[7] = 0.7589043;
inarray[7] = (inarray[7] + 0.8170462) / 1.575951;
 
if (inarray[8]<-0.7128723) inarray[8] = -0.7128723;
if (inarray[8]>0.5189937) inarray[8] = 0.5189937;
inarray[8] = (inarray[8] + 0.7128723) / 1.231866;
 
if (inarray[9]<-0.5390543) inarray[9] = -0.5390543;
if (inarray[9]>0.4367434) inarray[9] = 0.4367434;
inarray[9] = (inarray[9] + 0.5390543) / 0.9757977;
 
inarray[0] *= 0.4823529;
inarray[1] *= 0.09411765;
inarray[2] *= 2.376471;
inarray[3] *= 0.8588235;
inarray[4] *= 2.317647;
inarray[5] *= 1.741176;
inarray[6] *= 0.09411765;
inarray[7] *= 1.870588;
inarray[8] *= 1.305882;
inarray[9] *= 2.447059;
 
feature2[0] = MathPow(inarray[0] -0.000889163, 2);
feature2[0] += MathPow(inarray[1] -0.0812654, 2);
feature2[0] += MathPow(inarray[2] -1.960218, 2);
feature2[0] += MathPow(inarray[3] -0.2229109, 2);
feature2[0] += MathPow(inarray[4] -0.3805427, 2);
feature2[0] += MathPow(inarray[5] -0.445791, 2);
feature2[0] += MathPow(inarray[6] -0.05064157, 2);
feature2[0] += MathPow(inarray[7] -1.096478, 2);
feature2[0] += MathPow(inarray[8] -0.4719081, 2);
feature2[0] += MathPow(inarray[9] -0.4417005, 2);
feature2[0] = MathExp(-feature2[0] / 0.1861218);
 
feature2[1] = MathPow(inarray[0] -0.005004976, 2);
feature2[1] += MathPow(inarray[1] -0.001067986, 2);
feature2[1] += MathPow(inarray[2] -0.03456692, 2);
feature2[1] += MathPow(inarray[3] -0.0375561, 2);
feature2[1] += MathPow(inarray[4] -1.363077, 2);
feature2[1] += MathPow(inarray[5] -1.234163, 2);
feature2[1] += MathPow(inarray[6] -0.04992515, 2);
feature2[1] += MathPow(inarray[7] -1.326206, 2);
feature2[1] += MathPow(inarray[8] -1.108252, 2);
feature2[1] += MathPow(inarray[9] -1.952089, 2);
feature2[1] = MathExp(-feature2[1] / 0.1861218);
 
feature2[2] = MathPow(inarray[0] -0.02484213, 2);
feature2[2] += MathPow(inarray[1] -0.07485696, 2);
feature2[2] += MathPow(inarray[2] -2.221637, 2);
feature2[2] += MathPow(inarray[3] -0.7971078, 2);
feature2[2] += MathPow(inarray[4] -1.345066, 2);
feature2[2] += MathPow(inarray[5] -0.263566, 2);
feature2[2] += MathPow(inarray[6] -0.03033854, 2);
feature2[2] += MathPow(inarray[7] -0.4391496, 2);
feature2[2] += MathPow(inarray[8] -0.4931421, 2);
feature2[2] += MathPow(inarray[9] -1.405976, 2);
feature2[2] = MathExp(-feature2[2] / 0.1861218);
 
feature2[3] = MathPow(inarray[0] -0.477237, 2);
feature2[3] += MathPow(inarray[1] -0.02954985, 2);
feature2[3] += MathPow(inarray[2] -0.02116284, 2);
feature2[3] += MathPow(inarray[3] -0.07086566, 2);
feature2[3] += MathPow(inarray[4] -1.131745, 2);
feature2[3] += MathPow(inarray[5] -1.447294, 2);
feature2[3] += MathPow(inarray[6] -0.056236, 2);
feature2[3] += MathPow(inarray[7] -0.1194166, 2);
feature2[3] += MathPow(inarray[8] -0.000000063186, 2);
feature2[3] += MathPow(inarray[9] -0, 2);
feature2[3] = MathExp(-feature2[3] / 0.1861218);
 
feature2[4] = MathPow(inarray[0] -0.03997645, 2);
feature2[4] += MathPow(inarray[1] -0.008338865, 2);
feature2[4] += MathPow(inarray[2] -0.4439969, 2);
feature2[4] += MathPow(inarray[3] -0.2434077, 2);
feature2[4] += MathPow(inarray[4] -1.582111, 2);
feature2[4] += MathPow(inarray[5] -1.077382, 2);
feature2[4] += MathPow(inarray[6] -0.02703695, 2);
feature2[4] += MathPow(inarray[7] -0.7009466, 2);
feature2[4] += MathPow(inarray[8] -0.7786316, 2);
feature2[4] += MathPow(inarray[9] -1.821606, 2);
feature2[4] = MathExp(-feature2[4] / 0.1861218);
 
feature2[5] = MathPow(inarray[0] -0.03997645, 2);
feature2[5] += MathPow(inarray[1] -0.03795453, 2);
feature2[5] += MathPow(inarray[2] -0.4211179, 2);
feature2[5] += MathPow(inarray[3] -0.2650536, 2);
feature2[5] += MathPow(inarray[4] -1.442005, 2);
feature2[5] += MathPow(inarray[5] -1.215619, 2);
feature2[5] += MathPow(inarray[6] -0.05647669, 2);
feature2[5] += MathPow(inarray[7] -0.2027616, 2);
feature2[5] += MathPow(inarray[8] -0.1092168, 2);
feature2[5] += MathPow(inarray[9] -0.3853737, 2);
feature2[5] = MathExp(-feature2[5] / 0.1861218);
 
feature2[6] = MathPow(inarray[0] -0.1333482, 2);
feature2[6] += MathPow(inarray[1] -0.02954985, 2);
feature2[6] += MathPow(inarray[2] -0.4680712, 2);
feature2[6] += MathPow(inarray[3] -0.4329588, 2);
feature2[6] += MathPow(inarray[4] -1.331459, 2);
feature2[6] += MathPow(inarray[5] -0.6223203, 2);
feature2[6] += MathPow(inarray[6] -0.03947195, 2);
feature2[6] += MathPow(inarray[7] -0.8871927, 2);
feature2[6] += MathPow(inarray[8] -0.8125428, 2);
feature2[6] += MathPow(inarray[9] -1.284233, 2);
feature2[6] = MathExp(-feature2[6] / 0.1861218);
 
feature2[7] = MathPow(inarray[0] -0.002048658, 2);
feature2[7] += MathPow(inarray[1] -0.008338865, 2);
feature2[7] += MathPow(inarray[2] -1.348293, 2);
feature2[7] += MathPow(inarray[3] -0.1752524, 2);
feature2[7] += MathPow(inarray[4] -0.8967286, 2);
feature2[7] += MathPow(inarray[5] -0.4706461, 2);
feature2[7] += MathPow(inarray[6] -0.04881833, 2);
feature2[7] += MathPow(inarray[7] -1.141258, 2);
feature2[7] += MathPow(inarray[8] -0.5672179, 2);
feature2[7] += MathPow(inarray[9] -1.286871, 2);
feature2[7] = MathExp(-feature2[7] / 0.1861218);
 
feature2[8] = MathPow(inarray[0] -0.00856264, 2);
feature2[8] += MathPow(inarray[1] -0.03357983, 2);
feature2[8] += MathPow(inarray[2] -1.099166, 2);
feature2[8] += MathPow(inarray[3] -0.7331175, 2);
feature2[8] += MathPow(inarray[4] -1.943795, 2);
feature2[8] += MathPow(inarray[5] -1.505324, 2);
feature2[8] += MathPow(inarray[6] -0.06419788, 2);
feature2[8] += MathPow(inarray[7] -1.206465, 2);
feature2[8] += MathPow(inarray[8] -1.080792, 2);
feature2[8] += MathPow(inarray[9] -1.291121, 2);
feature2[8] = MathExp(-feature2[8] / 0.1861218);
 
feature2[9] = MathPow(inarray[0] -0.1333482, 2);
feature2[9] += MathPow(inarray[1] -0.07731364, 2);
feature2[9] += MathPow(inarray[2] -2.283653, 2);
feature2[9] += MathPow(inarray[3] -0.8243476, 2);
feature2[9] += MathPow(inarray[4] -2.180786, 2);
feature2[9] += MathPow(inarray[5] -1.427877, 2);
feature2[9] += MathPow(inarray[6] -0.06812724, 2);
feature2[9] += MathPow(inarray[7] -1.245306, 2);
feature2[9] += MathPow(inarray[8] -0.6635692, 2);
feature2[9] += MathPow(inarray[9] -1.431758, 2);
feature2[9] = MathExp(-feature2[9] / 0.1861218);
 
feature2[10] = MathPow(inarray[0] -0.0165808, 2);
feature2[10] += MathPow(inarray[1] -0.001067986, 2);
feature2[10] += MathPow(inarray[2] -0.02921889, 2);
feature2[10] += MathPow(inarray[3] -0.03873064, 2);
feature2[10] += MathPow(inarray[4] -0.2049543, 2);
feature2[10] += MathPow(inarray[5] -1.091212, 2);
feature2[10] += MathPow(inarray[6] -0.07272343, 2);
feature2[10] += MathPow(inarray[7] -0.4463593, 2);
feature2[10] += MathPow(inarray[8] -0.3753075, 2);
feature2[10] += MathPow(inarray[9] -1.12231, 2);
feature2[10] = MathExp(-feature2[10] / 0.1861218);
 
feature2[11] = MathPow(inarray[0] -0.2418543, 2);
feature2[11] += MathPow(inarray[1] -0.06880314, 2);
feature2[11] += MathPow(inarray[2] -0.04994078, 2);
feature2[11] += MathPow(inarray[3] -0.1397965, 2);
feature2[11] += MathPow(inarray[4] -0.4064695, 2);
feature2[11] += MathPow(inarray[5] -0.7573185, 2);
feature2[11] += MathPow(inarray[6] -0.02113329, 2);
feature2[11] += MathPow(inarray[7] -0.3229572, 2);
feature2[11] += MathPow(inarray[8] -0.3608783, 2);
feature2[11] += MathPow(inarray[9] -1.261533, 2);
feature2[11] = MathExp(-feature2[11] / 0.1861218);

feature2[12] = MathPow(inarray[0] -0.07029111, 2);
feature2[12] += MathPow(inarray[1] -0.0828398, 2);
feature2[12] += MathPow(inarray[2] -2.028337, 2);
feature2[12] += MathPow(inarray[3] -0.5885905, 2);
feature2[12] += MathPow(inarray[4] -0.5318826, 2);
feature2[12] += MathPow(inarray[5] -0.4206511, 2);
feature2[12] += MathPow(inarray[6] -0.03244338, 2);
feature2[12] += MathPow(inarray[7] -0.3602117, 2);
feature2[12] += MathPow(inarray[8] -0.577378, 2);
feature2[12] += MathPow(inarray[9] -1.761364, 2);
feature2[12] = MathExp(-feature2[12] / 0.1861218);

feature2[13] = MathPow(inarray[0] -0.4134175, 2);
feature2[13] += MathPow(inarray[1] -0.07202942, 2);
feature2[13] += MathPow(inarray[2] -2.044716, 2);
feature2[13] += MathPow(inarray[3] -0.7512222, 2);
feature2[13] += MathPow(inarray[4] -1.648931, 2);
feature2[13] += MathPow(inarray[5] -0.491877, 2);
feature2[13] += MathPow(inarray[6] -0.03325736, 2);
feature2[13] += MathPow(inarray[7] -0.6765239, 2);
feature2[13] += MathPow(inarray[8] -0.4526261, 2);
feature2[13] += MathPow(inarray[9] -1.342896, 2);
feature2[13] = MathExp(-feature2[13] / 0.1861218);

feature2[14] = MathPow(inarray[0] -0.01167812, 2);
feature2[14] += MathPow(inarray[1] -0.002026555, 2);
feature2[14] += MathPow(inarray[2] -1.305017, 2);
feature2[14] += MathPow(inarray[3] -0.7808279, 2);
feature2[14] += MathPow(inarray[4] -2.317647, 2);
feature2[14] += MathPow(inarray[5] -1.741176, 2);
feature2[14] += MathPow(inarray[6] -0.09411765, 2);
feature2[14] += MathPow(inarray[7] -1.824782, 2);
feature2[14] += MathPow(inarray[8] -0.9379704, 2);
feature2[14] += MathPow(inarray[9] -1.126272, 2);
feature2[14] = MathExp(-feature2[14] / 0.1861218);

feature2[15] = MathPow(inarray[0] -0.02484213, 2);
feature2[15] += MathPow(inarray[1] -0.01740354, 2);
feature2[15] += MathPow(inarray[2] -0.6076435, 2);
feature2[15] += MathPow(inarray[3] -0.1440797, 2);
feature2[15] += MathPow(inarray[4] -0.5070454, 2);
feature2[15] += MathPow(inarray[5] -0.5617672, 2);
feature2[15] += MathPow(inarray[6] -0.004748089, 2);
feature2[15] += MathPow(inarray[7] -0, 2);
feature2[15] += MathPow(inarray[8] -0.1619062, 2);
feature2[15] += MathPow(inarray[9] -1.714904, 2);
feature2[15] = MathExp(-feature2[15] / 0.1861218);

feature2[16] = MathPow(inarray[0] -0.4797696, 2);
feature2[16] += MathPow(inarray[1] -0.09336158, 2);
feature2[16] += MathPow(inarray[2] -2.356911, 2);
feature2[16] += MathPow(inarray[3] -0.0364001, 2);
feature2[16] += MathPow(inarray[4] -0.08372539, 2);
feature2[16] += MathPow(inarray[5] -0.106237, 2);
feature2[16] += MathPow(inarray[6] -0.007292937, 2);
feature2[16] += MathPow(inarray[7] -0.2185573, 2);
feature2[16] += MathPow(inarray[8] -0.6566861, 2);
feature2[16] += MathPow(inarray[9] -1.550643, 2);
feature2[16] = MathExp(-feature2[16] / 0.1861218);

feature2[17] = MathPow(inarray[0] -0.003140907, 2);
feature2[17] += MathPow(inarray[1] -0.003383724, 2);
feature2[17] += MathPow(inarray[2] -0.07914829, 2);
feature2[17] += MathPow(inarray[3] -0.3211505, 2);
feature2[17] += MathPow(inarray[4] -1.134912, 2);
feature2[17] += MathPow(inarray[5] -0.9465042, 2);
feature2[17] += MathPow(inarray[6] -0.07714743, 2);
feature2[17] += MathPow(inarray[7] -1.670727, 2);
feature2[17] += MathPow(inarray[8] -1.127436, 2);
feature2[17] += MathPow(inarray[9] -1.75758, 2);
feature2[17] = MathExp(-feature2[17] / 0.1861218);

feature2[18] = MathPow(inarray[0] -0.1333482, 2);
feature2[18] += MathPow(inarray[1] -0.004925451, 2);
feature2[18] += MathPow(inarray[2] -2.358818, 2);
feature2[18] += MathPow(inarray[3] -0.01149535, 2);
feature2[18] += MathPow(inarray[4] -1.904187, 2);
feature2[18] += MathPow(inarray[5] -1.600977, 2);
feature2[18] += MathPow(inarray[6] -0.08518686, 2);
feature2[18] += MathPow(inarray[7] -1.7597, 2);
feature2[18] += MathPow(inarray[8] -0.7659574, 2);
feature2[18] += MathPow(inarray[9] -1.493686, 2);
feature2[18] = MathExp(-feature2[18] / 0.1861218);

feature2[19] = MathPow(inarray[0] -0.0000000144617, 2);
feature2[19] += MathPow(inarray[1] -0.00000000285813, 2);
feature2[19] += MathPow(inarray[2] -0.0000000739352, 2);
feature2[19] += MathPow(inarray[3] -0, 2);
feature2[19] += MathPow(inarray[4] -0.01601893, 2);
feature2[19] += MathPow(inarray[5] -0.05264656, 2);
feature2[19] += MathPow(inarray[6] -0.01949112, 2);
feature2[19] += MathPow(inarray[7] -1.165342, 2);
feature2[19] += MathPow(inarray[8] -1.00786, 2);
feature2[19] += MathPow(inarray[9] -1.539615, 2);
feature2[19] = MathExp(-feature2[19] / 0.1861218);

feature2[20] = MathPow(inarray[0] -0.000320225, 2);
feature2[20] += MathPow(inarray[1] -0.0000189151, 2);
feature2[20] += MathPow(inarray[2] -0.01893473, 2);
feature2[20] += MathPow(inarray[3] -0.01383689, 2);
feature2[20] += MathPow(inarray[4] -0, 2);
feature2[20] += MathPow(inarray[5] -0, 2);
feature2[20] += MathPow(inarray[6] -0.02966899, 2);
feature2[20] += MathPow(inarray[7] -0.9710125, 2);
feature2[20] += MathPow(inarray[8] -0.9873776, 2);
feature2[20] += MathPow(inarray[9] -2.161196, 2);
feature2[20] = MathExp(-feature2[20] / 0.1861218);

feature2[21] = MathPow(inarray[0] -0.005004976, 2);
feature2[21] += MathPow(inarray[1] -0.002836758, 2);
feature2[21] += MathPow(inarray[2] -0.04994078, 2);
feature2[21] += MathPow(inarray[3] -0.03249545, 2);
feature2[21] += MathPow(inarray[4] -1.357091, 2);
feature2[21] += MathPow(inarray[5] -1.343082, 2);
feature2[21] += MathPow(inarray[6] -0.06585187, 2);
feature2[21] += MathPow(inarray[7] -1.202506, 2);
feature2[21] += MathPow(inarray[8] -0.9318905, 2);
feature2[21] += MathPow(inarray[9] -0.4229908, 2);
feature2[21] = MathExp(-feature2[21] / 0.1861218);

feature2[22] = MathPow(inarray[0] -0.4588665, 2);
feature2[22] += MathPow(inarray[1] -0.09065208, 2);
feature2[22] += MathPow(inarray[2] -2.286221, 2);
feature2[22] += MathPow(inarray[3] -0.7106162, 2);
feature2[22] += MathPow(inarray[4] -1.968035, 2);
feature2[22] += MathPow(inarray[5] -1.458471, 2);
feature2[22] += MathPow(inarray[6] -0.04956202, 2);
feature2[22] += MathPow(inarray[7] -1.257551, 2);
feature2[22] += MathPow(inarray[8] -0.8764666, 2);
feature2[22] += MathPow(inarray[9] -0.5849004, 2);
feature2[22] = MathExp(-feature2[22] / 0.1861218);

feature2[23] = MathPow(inarray[0] -0.4671278, 2);
feature2[23] += MathPow(inarray[1] -0.09024769, 2);
feature2[23] += MathPow(inarray[2] -2.217067, 2);
feature2[23] += MathPow(inarray[3] -0.6357771, 2);
feature2[23] += MathPow(inarray[4] -0.1898238, 2);
feature2[23] += MathPow(inarray[5] -0.1001203, 2);
feature2[23] += MathPow(inarray[6] -0.007896511, 2);
feature2[23] += MathPow(inarray[7] -0.5215328, 2);
feature2[23] += MathPow(inarray[8] -0.5950916, 2);
feature2[23] += MathPow(inarray[9] -1.563175, 2);
feature2[23] = MathExp(-feature2[23] / 0.1861218);

feature2[24] = MathPow(inarray[0] -0.01167812, 2);
feature2[24] += MathPow(inarray[1] -0.005441452, 2);
feature2[24] += MathPow(inarray[2] -0.2351743, 2);
feature2[24] += MathPow(inarray[3] -0.01383689, 2);
feature2[24] += MathPow(inarray[4] -0.1531568, 2);
feature2[24] += MathPow(inarray[5] -0.3552061, 2);
feature2[24] += MathPow(inarray[6] -0.04040769, 2);
feature2[24] += MathPow(inarray[7] -1.257551, 2);
feature2[24] += MathPow(inarray[8] -0.9742168, 2);
feature2[24] += MathPow(inarray[9] -1.728614, 2);
feature2[24] = MathExp(-feature2[24] / 0.1861218);

feature2[25] = MathPow(inarray[0] -0.4437321, 2);
feature2[25] += MathPow(inarray[1] -0.01052124, 2);
feature2[25] += MathPow(inarray[2] -0.08071024, 2);
feature2[25] += MathPow(inarray[3] -0.02579022, 2);
feature2[25] += MathPow(inarray[4] -0.1812214, 2);
feature2[25] += MathPow(inarray[5] -0.3193101, 2);
feature2[25] += MathPow(inarray[6] -0.02369904, 2);
feature2[25] += MathPow(inarray[7] -0.7097268, 2);
feature2[25] += MathPow(inarray[8] -0.7359379, 2);
feature2[25] += MathPow(inarray[9] -1.762847, 2);
feature2[25] = MathExp(-feature2[25] / 0.1861218);

feature2[26] = MathPow(inarray[0] -0.2418543, 2);
feature2[26] += MathPow(inarray[1] -0.05212994, 2);
feature2[26] += MathPow(inarray[2] -0.6616061, 2);
feature2[26] += MathPow(inarray[3] -0.2434077, 2);
feature2[26] += MathPow(inarray[4] -0.9345729, 2);
feature2[26] += MathPow(inarray[5] -0.6033763, 2);
feature2[26] += MathPow(inarray[6] -0.03585919, 2);
feature2[26] += MathPow(inarray[7] -0.946506, 2);
feature2[26] += MathPow(inarray[8] -0.727547, 2);
feature2[26] += MathPow(inarray[9] -1.323893, 2);
feature2[26] = MathExp(-feature2[26] / 0.1861218);

feature2[27] = MathPow(inarray[0] -0.4134175, 2);
feature2[27] += MathPow(inarray[1] -0.06880314, 2);
feature2[27] += MathPow(inarray[2] -2.376471, 2);
feature2[27] += MathPow(inarray[3] -0.8588235, 2);
feature2[27] += MathPow(inarray[4] -2.216153, 2);
feature2[27] += MathPow(inarray[5] -1.636531, 2);
feature2[27] += MathPow(inarray[6] -0.08925647, 2);
feature2[27] += MathPow(inarray[7] -1.73262, 2);
feature2[27] += MathPow(inarray[8] -0.9227096, 2);
feature2[27] += MathPow(inarray[9] -0.7684286, 2);
feature2[27] = MathExp(-feature2[27] / 0.1861218);

feature2[28] = MathPow(inarray[0] -0.003140907, 2);
feature2[28] += MathPow(inarray[1] -0.002026555, 2);
feature2[28] += MathPow(inarray[2] -0.1903487, 2);
feature2[28] += MathPow(inarray[3] -0.1101131, 2);
feature2[28] += MathPow(inarray[4] -0.5755196, 2);
feature2[28] += MathPow(inarray[5] -0.8396258, 2);
feature2[28] += MathPow(inarray[6] -0.06927142, 2);
feature2[28] += MathPow(inarray[7] -1.19529, 2);
feature2[28] += MathPow(inarray[8] -0.9351867, 2);
feature2[28] += MathPow(inarray[9] -1.508576, 2);
feature2[28] = MathExp(-feature2[28] / 0.1861218);

feature2[29] = MathPow(inarray[0] -0.006471551, 2);
feature2[29] += MathPow(inarray[1] -0.004469482, 2);
feature2[29] += MathPow(inarray[2] -0.2707826, 2);
feature2[29] += MathPow(inarray[3] -0.03358909, 2);
feature2[29] += MathPow(inarray[4] -0.4098204, 2);
feature2[29] += MathPow(inarray[5] -0.2399384, 2);
feature2[29] += MathPow(inarray[6] -0.02301284, 2);
feature2[29] += MathPow(inarray[7] -0.9791961, 2);
feature2[29] += MathPow(inarray[8] -0.7661448, 2);
feature2[29] += MathPow(inarray[9] -1.484248, 2);
feature2[29] = MathExp(-feature2[29] / 0.1861218);

feature2[30] = MathPow(inarray[0] -0.4588665, 2);
feature2[30] += MathPow(inarray[1] -0.07485696, 2);
feature2[30] += MathPow(inarray[2] -0.5973035, 2);
feature2[30] += MathPow(inarray[3] -0.01670468, 2);
feature2[30] += MathPow(inarray[4] -0.2150007, 2);
feature2[30] += MathPow(inarray[5] -0.1481559, 2);
feature2[30] += MathPow(inarray[6] -0.01565715, 2);
feature2[30] += MathPow(inarray[7] -0.6264769, 2);
feature2[30] += MathPow(inarray[8] -0.8419943, 2);
feature2[30] += MathPow(inarray[9] -1.380335, 2);
feature2[30] = MathExp(-feature2[30] / 0.1861218);

feature2[31] = MathPow(inarray[0] -0.03997645, 2);
feature2[31] += MathPow(inarray[1] -0.01052124, 2);
feature2[31] += MathPow(inarray[2] -1.513642, 2);
feature2[31] += MathPow(inarray[3] -0.8544291, 2);
feature2[31] += MathPow(inarray[4] -2.296429, 2);
feature2[31] += MathPow(inarray[5] -1.678599, 2);
feature2[31] += MathPow(inarray[6] -0.06536879, 2);
feature2[31] += MathPow(inarray[7] -0.4521235, 2);
feature2[31] += MathPow(inarray[8] -0.5051789, 2);
feature2[31] += MathPow(inarray[9] -1.82941, 2);
feature2[31] = MathExp(-feature2[31] / 0.1861218);

feature2[32] = MathPow(inarray[0] -0.003938961, 2);
feature2[32] += MathPow(inarray[1] -0.02954985, 2);
feature2[32] += MathPow(inarray[2] -1.44653, 2);
feature2[32] += MathPow(inarray[3] -0.8099366, 2);
feature2[32] += MathPow(inarray[4] -2.201304, 2);
feature2[32] += MathPow(inarray[5] -1.492814, 2);
feature2[32] += MathPow(inarray[6] -0.08035071, 2);
feature2[32] += MathPow(inarray[7] -1.291567, 2);
feature2[32] += MathPow(inarray[8] -0.7814157, 2);
feature2[32] += MathPow(inarray[9] -1.187166, 2);
feature2[32] = MathExp(-feature2[32] / 0.1861218);

feature2[33] = MathPow(inarray[0] -0.03997645, 2);
feature2[33] += MathPow(inarray[1] -0.01345177, 2);
feature2[33] += MathPow(inarray[2] -0.0888809, 2);
feature2[33] += MathPow(inarray[3] -0.09145273, 2);
feature2[33] += MathPow(inarray[4] -0.9544851, 2);
feature2[33] += MathPow(inarray[5] -0.6133671, 2);
feature2[33] += MathPow(inarray[6] -0.08232641, 2);
feature2[33] += MathPow(inarray[7] -1.560351, 2);
feature2[33] += MathPow(inarray[8] -0.771435, 2);
feature2[33] += MathPow(inarray[9] -0.8860366, 2);
feature2[33] = MathExp(-feature2[33] / 0.1861218);

feature2[34] = MathPow(inarray[0] -0.4134175, 2);
feature2[34] += MathPow(inarray[1] -0.01986022, 2);
feature2[34] += MathPow(inarray[2] -1.158104, 2);
feature2[34] += MathPow(inarray[3] -0.598632, 2);
feature2[34] += MathPow(inarray[4] -2.118678, 2);
feature2[34] += MathPow(inarray[5] -1.524671, 2);
feature2[34] += MathPow(inarray[6] -0.08608676, 2);
feature2[34] += MathPow(inarray[7] -1.870588, 2);
feature2[34] += MathPow(inarray[8] -0.8369381, 2);
feature2[34] += MathPow(inarray[9] -0.7220582, 2);
feature2[34] = MathExp(-feature2[34] / 0.1861218);

feature2[35] = MathPow(inarray[0] -0.4588665, 2);
feature2[35] += MathPow(inarray[1] -0.03357983, 2);
feature2[35] += MathPow(inarray[2] -1.602334, 2);
feature2[35] += MathPow(inarray[3] -0.2717606, 2);
feature2[35] += MathPow(inarray[4] -0.1902421, 2);
feature2[35] += MathPow(inarray[5] -0.00084889, 2);
feature2[35] += MathPow(inarray[6] -0, 2);
feature2[35] += MathPow(inarray[7] -0.259377, 2);
feature2[35] += MathPow(inarray[8] -0.7457957, 2);
feature2[35] += MathPow(inarray[9] -1.958541, 2);
feature2[35] = MathExp(-feature2[35] / 0.1861218);

feature2[36] = MathPow(inarray[0] -0.005004976, 2);
feature2[36] += MathPow(inarray[1] -0.002392147, 2);
feature2[36] += MathPow(inarray[2] -0.01608778, 2);
feature2[36] += MathPow(inarray[3] -0.1124831, 2);
feature2[36] += MathPow(inarray[4] -1.982063, 2);
feature2[36] += MathPow(inarray[5] -1.456154, 2);
feature2[36] += MathPow(inarray[6] -0.08319337, 2);
feature2[36] += MathPow(inarray[7] -1.753423, 2);
feature2[36] += MathPow(inarray[8] -1.045702, 2);
feature2[36] += MathPow(inarray[9] -0.9721034, 2);
feature2[36] = MathExp(-feature2[36] / 0.1861218);

feature2[37] = MathPow(inarray[0] -0.0165808, 2);
feature2[37] += MathPow(inarray[1] -0.01345177, 2);
feature2[37] += MathPow(inarray[2] -0.9129646, 2);
feature2[37] += MathPow(inarray[3] -0.007112046, 2);
feature2[37] += MathPow(inarray[4] -0.05920475, 2);
feature2[37] += MathPow(inarray[5] -0.08653653, 2);
feature2[37] += MathPow(inarray[6] -0.01888973, 2);
feature2[37] += MathPow(inarray[7] -0.2948249, 2);
feature2[37] += MathPow(inarray[8] -0.6147208, 2);
feature2[37] += MathPow(inarray[9] -1.940528, 2);
feature2[37] = MathExp(-feature2[37] / 0.1861218);

outarray[0] = feature2[3];
outarray[0] += feature2[10];
outarray[0] += feature2[11];
outarray[0] += feature2[12];
outarray[0] += feature2[13];
outarray[0] += feature2[15];
outarray[0] += feature2[16];
outarray[0] += feature2[22];
outarray[0] += feature2[23];
outarray[0] += feature2[25];
outarray[0] += feature2[26];
outarray[0] += feature2[29];
outarray[0] += feature2[30];
outarray[0] += feature2[35];
outarray[0] += feature2[37];
outarray[0] /= 15;

}
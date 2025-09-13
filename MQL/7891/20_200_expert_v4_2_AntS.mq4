//+--------------------------------------------------------------------+
//|                                            20/200 expert v3.mq4    |
//|                                                    1H   EUR/USD    |
//|                                                    Smirnov Pavel   |
//|                                                 www.autoforex.ru   |
//| The original EA by Pavel Smirnoy, modified, with a quite proper    |
//| optimization and additional function of the automated lot size.    |
//| And lot increasing to cover losses. After modification the EA      |
//| behaviour is not bad. But is still nonproductive. One can earn     |
//| more trading mamnually. tested in reality. Works like in the       |
//| tester. Recommended for a deposit from $10000. With lower          |
//| deposite profit is too small. Only for EUR/USD on 1H chart!!!!     |
//|                                                               AntS |
//+--------------------------------------------------------------------+

#property copyright "Smirnov Pavel"
#property link      "www.autoforex.ru"

 int TakeProfit_L = 39; // Take Profit in points
 int StopLoss_L = 147;  // Stop Loss in points
 int TakeProfit_S = 32; // Take Profit in points
 int StopLoss_S = 267;  // Stop Loss in points
 int TradeTime=18;      // Time to enter the market
 int t1=6;              
 int t2=2;                
 int delta_L=6;         
 int delta_S=21;         
extern double lot = 0.01;      // Lot size
 int Orders=1;          // maximal number of positions opened at a time
 int MaxOpenTime=504;
 int BigLotSize = 6;    // By how much lot size is multiplicated in Big lot
extern bool AutoLot=true;

int ticket,total,cnt;
bool cantrade=true;
double closeprice;
double tmp;

int LotSize()
// The function opens a short position with lot size=volume
{
if (AccountBalance()>=300) lot=0.01;
if (AccountBalance()>=500) lot=0.02;
if (AccountBalance()>=800) lot=0.03;
if (AccountBalance()>=1000) lot=0.04;
if (AccountBalance()>=1300) lot=0.05;
if (AccountBalance()>=1600) lot=0.06;
if (AccountBalance()>=1800) lot=0.07;
if (AccountBalance()>=2100) lot=0.08;
if (AccountBalance()>=2400) lot=0.09;
if (AccountBalance()>=2700) lot=0.10;
if (AccountBalance()>=3000) lot=0.11;
if (AccountBalance()>=3300) lot=0.12;
if (AccountBalance()>=3500) lot=0.13;
if (AccountBalance()>=3785) lot=0.14;
if (AccountBalance()>=4058) lot=0.15;
if (AccountBalance()>=4332) lot=0.16;
if (AccountBalance()>=4605) lot=0.17;
if (AccountBalance()>=4879) lot=0.18;
if (AccountBalance()>=5153) lot=0.19;
if (AccountBalance()>=5626) lot=0.20;
if (AccountBalance()>=5700) lot=0.21;
if (AccountBalance()>=5974) lot=0.22;
if (AccountBalance()>=6247) lot=0.23;
if (AccountBalance()>=6521) lot=0.24;
if (AccountBalance()>=6795) lot=0.25;
if (AccountBalance()>=7068) lot=0.26;
if (AccountBalance()>=7342) lot=0.27;
if (AccountBalance()>=7615) lot=0.28;
if (AccountBalance()>=7889) lot=0.29;
if (AccountBalance()>=8163) lot=0.30;
if (AccountBalance()>=8436) lot=0.31;
if (AccountBalance()>=8710) lot=0.32;
if (AccountBalance()>=8984) lot=0.33;
if (AccountBalance()>=9257) lot=0.34;
if (AccountBalance()>=9531) lot=0.35;
if (AccountBalance()>=9804) lot=0.36;
if (AccountBalance()>=10078) lot=0.37;
if (AccountBalance()>=10352) lot=0.38;
if (AccountBalance()>=10625) lot=0.39;
if (AccountBalance()>=10899) lot=0.40;
if (AccountBalance()>=11173) lot=0.41;
if (AccountBalance()>=11446) lot=0.42;
if (AccountBalance()>=11720) lot=0.43;
if (AccountBalance()>=11993) lot=0.44;
if (AccountBalance()>=12267) lot=0.45;
if (AccountBalance()>=12541) lot=0.46;
if (AccountBalance()>=12814) lot=0.47;
if (AccountBalance()>=13088) lot=0.48;
if (AccountBalance()>=13362) lot=0.49;
if (AccountBalance()>=13635) lot=0.50;
if (AccountBalance()>=13909) lot=0.51;
if (AccountBalance()>=14182) lot=0.52;
if (AccountBalance()>=14456) lot=0.53;
if (AccountBalance()>=14730) lot=0.54;
if (AccountBalance()>=15003) lot=0.55;
if (AccountBalance()>=15277) lot=0.56;
if (AccountBalance()>=15551) lot=0.57;
if (AccountBalance()>=15824) lot=0.58;
if (AccountBalance()>=16098) lot=0.59;
if (AccountBalance()>=16371) lot=0.60;
if (AccountBalance()>=16645) lot=0.61;
if (AccountBalance()>=16919) lot=0.62;
if (AccountBalance()>=17192) lot=0.63;
if (AccountBalance()>=17466) lot=0.64;
if (AccountBalance()>=17740) lot=0.65;
if (AccountBalance()>=18013) lot=0.66;
if (AccountBalance()>=18287) lot=0.67;
if (AccountBalance()>=18560) lot=0.68;
if (AccountBalance()>=18834) lot=0.69;
if (AccountBalance()>=19108) lot=0.70;
if (AccountBalance()>=19381) lot=0.71;
if (AccountBalance()>=19655) lot=0.72;
if (AccountBalance()>=19929) lot=0.73;
if (AccountBalance()>=20202) lot=0.74;
if (AccountBalance()>=20476) lot=0.75;
if (AccountBalance()>=20749) lot=0.76;
if (AccountBalance()>=21023) lot=0.77;
if (AccountBalance()>=21297) lot=0.78;
if (AccountBalance()>=21570) lot=0.79;
if (AccountBalance()>=21844) lot=0.80;
if (AccountBalance()>=22118) lot=0.81;
if (AccountBalance()>=22391) lot=0.82;
if (AccountBalance()>=22665) lot=0.83;
if (AccountBalance()>=22938) lot=0.84;
if (AccountBalance()>=23212) lot=0.85;
if (AccountBalance()>=23486) lot=0.86;
if (AccountBalance()>=23759) lot=0.87;
if (AccountBalance()>=24033) lot=0.88;
if (AccountBalance()>=24307) lot=0.89;
if (AccountBalance()>=24580) lot=0.90;
if (AccountBalance()>=24854) lot=0.91;
if (AccountBalance()>=25127) lot=0.92;
if (AccountBalance()>=25401) lot=0.93;
if (AccountBalance()>=25675) lot=0.94;
if (AccountBalance()>=25948) lot=0.95;
if (AccountBalance()>=26222) lot=0.96;
if (AccountBalance()>=26496) lot=0.97;
if (AccountBalance()>=26795) lot=0.98;
if (AccountBalance()>=27043) lot=0.99;
if (AccountBalance()>=27316) lot=1.00;
if (AccountBalance()>=27590) lot=1.01;
if (AccountBalance()>=27864) lot=1.02;
if (AccountBalance()>=28137) lot=1.03;
if (AccountBalance()>=28411) lot=1.04;
if (AccountBalance()>=28685) lot=1.05;
if (AccountBalance()>=28958) lot=1.06;
if (AccountBalance()>=29232) lot=1.07;
if (AccountBalance()>=29505) lot=1.08;
if (AccountBalance()>=29779) lot=1.09;
if (AccountBalance()>=30053) lot=1.10;
if (AccountBalance()>=30326) lot=1.11;
if (AccountBalance()>=30600) lot=1.12;
if (AccountBalance()>=30874) lot=1.13;
if (AccountBalance()>=31147) lot=1.14;
if (AccountBalance()>=31421) lot=1.15;
if (AccountBalance()>=31695) lot=1.16;
if (AccountBalance()>=31968) lot=1.17;
if (AccountBalance()>=32242) lot=1.18;
if (AccountBalance()>=32515) lot=1.19;
if (AccountBalance()>=32789) lot=1.20;
if (AccountBalance()>=33063) lot=1.21;
if (AccountBalance()>=33336) lot=1.22;
if (AccountBalance()>=33610) lot=1.23;
if (AccountBalance()>=33884) lot=1.24;
if (AccountBalance()>=34157) lot=1.25;
if (AccountBalance()>=34431) lot=1.26;
if (AccountBalance()>=34704) lot=1.27;
if (AccountBalance()>=34978) lot=1.28;
if (AccountBalance()>=35252) lot=1.29;
if (AccountBalance()>=35525) lot=1.30;
if (AccountBalance()>=35799) lot=1.31;
if (AccountBalance()>=36073) lot=1.32;
if (AccountBalance()>=36346) lot=1.33;
if (AccountBalance()>=36620) lot=1.34;
if (AccountBalance()>=36893) lot=1.35;
if (AccountBalance()>=37167) lot=1.36;
if (AccountBalance()>=37441) lot=1.37;
if (AccountBalance()>=	37714	) lot=	1.38	;
if (AccountBalance()>=	37988	) lot=	1.39	;
if (AccountBalance()>=	38262	) lot=	1.40	;
if (AccountBalance()>=	38535	) lot=	1.41	;
if (AccountBalance()>=	38809	) lot=	1.42	;
if (AccountBalance()>=	39082	) lot=	1.43	;
if (AccountBalance()>=	39356	) lot=	1.44	;
if (AccountBalance()>=	39630	) lot=	1.45	;
if (AccountBalance()>=	39903	) lot=	1.46	;
if (AccountBalance()>=	40177	) lot=	1.47	;
if (AccountBalance()>=	40451	) lot=	1.48	;
if (AccountBalance()>=	40724	) lot=	1.49	;
if (AccountBalance()>=	40998	) lot=	1.50	;
if (AccountBalance()>=	41271	) lot=	1.51	;
if (AccountBalance()>=	41545	) lot=	1.52	;
if (AccountBalance()>=	41819	) lot=	1.53	;
if (AccountBalance()>=	42092	) lot=	1.54	;
if (AccountBalance()>=	42366	) lot=	1.55	;
if (AccountBalance()>=	42640	) lot=	1.56	;
if (AccountBalance()>=	42913	) lot=	1.57	;
if (AccountBalance()>=	43187	) lot=	1.58	;
if (AccountBalance()>=	43460	) lot=	1.59	;
if (AccountBalance()>=	43734	) lot=	1.60	;
if (AccountBalance()>=	44008	) lot=	1.61	;
if (AccountBalance()>=	44281	) lot=	1.62	;
if (AccountBalance()>=	44555	) lot=	1.63	;
if (AccountBalance()>=	44829	) lot=	1.64	;
if (AccountBalance()>=	45102	) lot=	1.65	;
if (AccountBalance()>=	45376	) lot=	1.66	;
if (AccountBalance()>=	45649	) lot=	1.67	;
if (AccountBalance()>=	45923	) lot=	1.68	;
if (AccountBalance()>=	46197	) lot=	1.69	;
if (AccountBalance()>=	46470	) lot=	1.70	;
if (AccountBalance()>=	46744	) lot=	1.71	;
if (AccountBalance()>=	47018	) lot=	1.72	;
if (AccountBalance()>=	47291	) lot=	1.73	;
if (AccountBalance()>=	47565	) lot=	1.74	;
if (AccountBalance()>=	47838	) lot=	1.75	;
if (AccountBalance()>=	48112	) lot=	1.76	;
if (AccountBalance()>=	48386	) lot=	1.77	;
if (AccountBalance()>=	48659	) lot=	1.78	;
if (AccountBalance()>=	48933	) lot=	1.79	;
if (AccountBalance()>=	49207	) lot=	1.80	;
if (AccountBalance()>=	49480	) lot=	1.81	;
if (AccountBalance()>=	49754	) lot=	1.82	;
if (AccountBalance()>=	50027	) lot=	1.83	;
if (AccountBalance()>=	50301	) lot=	1.84	;
if (AccountBalance()>=	50575	) lot=	1.85	;
if (AccountBalance()>=	50848	) lot=	1.86	;
if (AccountBalance()>=	51122	) lot=	1.87	;
if (AccountBalance()>=	51396	) lot=	1.88	;
if (AccountBalance()>=	51669	) lot=	1.89	;
if (AccountBalance()>=	51943	) lot=	1.90	;
if (AccountBalance()>=	52216	) lot=	1.91	;
if (AccountBalance()>=	52490	) lot=	1.92	;
if (AccountBalance()>=	52764	) lot=	1.93	;
if (AccountBalance()>=	53037	) lot=	1.94	;
if (AccountBalance()>=	53311	) lot=	1.95	;
if (AccountBalance()>=	53585	) lot=	1.96	;
if (AccountBalance()>=	53858	) lot=	1.97	;
if (AccountBalance()>=	54132	) lot=	1.98	;
if (AccountBalance()>=	54405	) lot=	1.99	;
if (AccountBalance()>=	54679	) lot=	2.00	;
if (AccountBalance()>=	54953	) lot=	2.01	;
if (AccountBalance()>=	55226	) lot=	2.02	;
if (AccountBalance()>=	55500	) lot=	2.03	;
if (AccountBalance()>=	55774	) lot=	2.04	;
if (AccountBalance()>=	56047	) lot=	2.05	;
if (AccountBalance()>=	56321	) lot=	2.06	;
if (AccountBalance()>=	56595	) lot=	2.07	;
if (AccountBalance()>=	56868	) lot=	2.08	;
if (AccountBalance()>=	57142	) lot=	2.09	;
if (AccountBalance()>=	57415	) lot=	2.10	;
if (AccountBalance()>=	57689	) lot=	2.11	;
if (AccountBalance()>=	57963	) lot=	2.12	;
if (AccountBalance()>=	58236	) lot=	2.13	;
if (AccountBalance()>=	58510	) lot=	2.14	;
if (AccountBalance()>=	58784	) lot=	2.15	;
if (AccountBalance()>=	59057	) lot=	2.16	;
if (AccountBalance()>=	59331	) lot=	2.17	;
if (AccountBalance()>=	59604	) lot=	2.18	;
if (AccountBalance()>=	59878	) lot=	2.19	;
if (AccountBalance()>=	60152	) lot=	2.20	;
if (AccountBalance()>=	60425	) lot=	2.21	;
if (AccountBalance()>=	60699	) lot=	2.22	;
if (AccountBalance()>=	60973	) lot=	2.23	;
if (AccountBalance()>=	61246	) lot=	2.24	;
if (AccountBalance()>=	61520	) lot=	2.25	;
if (AccountBalance()>=	61793	) lot=	2.26	;
if (AccountBalance()>=	62067	) lot=	2.27	;
if (AccountBalance()>=	62341	) lot=	2.28	;
if (AccountBalance()>=	62614	) lot=	2.29	;
if (AccountBalance()>=	62888	) lot=	2.30	;
if (AccountBalance()>=	63162	) lot=	2.31	;
if (AccountBalance()>=	63435	) lot=	2.32	;
if (AccountBalance()>=	63709	) lot=	2.33	;
if (AccountBalance()>=	63982	) lot=	2.34	;
if (AccountBalance()>=	64256	) lot=	2.35	;
if (AccountBalance()>=	64530	) lot=	2.36	;
if (AccountBalance()>=	64803	) lot=	2.37	;
if (AccountBalance()>=	65077	) lot=	2.38	;
if (AccountBalance()>=	65351	) lot=	2.39	;
if (AccountBalance()>=	65624	) lot=	2.40	;
if (AccountBalance()>=	65898	) lot=	2.41	;
if (AccountBalance()>=	66171	) lot=	2.42	;
if (AccountBalance()>=	66445	) lot=	2.43	;
if (AccountBalance()>=	66719	) lot=	2.44	;
if (AccountBalance()>=	66992	) lot=	2.45	;
if (AccountBalance()>=	67266	) lot=	2.46	;
if (AccountBalance()>=	67540	) lot=	2.47	;
if (AccountBalance()>=	67813	) lot=	2.48	;
if (AccountBalance()>=	68087	) lot=	2.49	;
if (AccountBalance()>=	68360	) lot=	2.50	;
if (AccountBalance()>=	68634	) lot=	2.51	;
if (AccountBalance()>=	68908	) lot=	2.52	;
if (AccountBalance()>=	69181	) lot=	2.53	;
if (AccountBalance()>=	69455	) lot=	2.54	;
if (AccountBalance()>=	69729	) lot=	2.55	;
if (AccountBalance()>=	70002	) lot=	2.56	;
if (AccountBalance()>=	70276	) lot=	2.57	;
if (AccountBalance()>=	70549	) lot=	2.58	;
if (AccountBalance()>=	70823	) lot=	2.59	;
if (AccountBalance()>=	71097	) lot=	2.60	;
if (AccountBalance()>=	71370	) lot=	2.61	;
if (AccountBalance()>=	71644	) lot=	2.62	;
if (AccountBalance()>=	71918	) lot=	2.63	;
if (AccountBalance()>=	72191	) lot=	2.64	;
if (AccountBalance()>=	72465	) lot=	2.65	;
if (AccountBalance()>=	72738	) lot=	2.66	;
if (AccountBalance()>=	73012	) lot=	2.67	;
if (AccountBalance()>=	73286	) lot=	2.68	;
if (AccountBalance()>=	73559	) lot=	2.69	;
if (AccountBalance()>=	73833	) lot=	2.70	;
if (AccountBalance()>=	74107	) lot=	2.71	;
if (AccountBalance()>=	74380	) lot=	2.72	;
if (AccountBalance()>=	74654	) lot=	2.73	;
if (AccountBalance()>=	74927	) lot=	2.74	;
if (AccountBalance()>=	75201	) lot=	2.75	;
if (AccountBalance()>=	75475	) lot=	2.76	;
if (AccountBalance()>=	75748	) lot=	2.77	;
if (AccountBalance()>=	76022	) lot=	2.78	;
if (AccountBalance()>=	76296	) lot=	2.79	;
if (AccountBalance()>=	76569	) lot=	2.80	;
if (AccountBalance()>=	76843	) lot=	2.81	;
if (AccountBalance()>=	77116	) lot=	2.82	;
if (AccountBalance()>=	77390	) lot=	2.83	;
if (AccountBalance()>=	77664	) lot=	2.84	;
if (AccountBalance()>=	77937	) lot=	2.85	;
if (AccountBalance()>=	78211	) lot=	2.86	;
if (AccountBalance()>=	78485	) lot=	2.87	;
if (AccountBalance()>=	78758	) lot=	2.88	;
if (AccountBalance()>=	79032	) lot=	2.89	;
if (AccountBalance()>=	79305	) lot=	2.90	;
if (AccountBalance()>=	79579	) lot=	2.91	;
if (AccountBalance()>=	79853	) lot=	2.92	;
if (AccountBalance()>=	80126	) lot=	2.93	;
if (AccountBalance()>=	80400	) lot=	2.94	;
if (AccountBalance()>=	80674	) lot=	2.95	;
if (AccountBalance()>=	80947	) lot=	2.96	;
if (AccountBalance()>=	81221	) lot=	2.97	;
if (AccountBalance()>=	81495	) lot=	2.98	;
if (AccountBalance()>=	81768	) lot=	2.99	;
if (AccountBalance()>=	82042	) lot=	3.00	;
if (AccountBalance()>=	82315	) lot=	3.01	;
if (AccountBalance()>=	82589	) lot=	3.02	;
if (AccountBalance()>=	82863	) lot=	3.03	;
if (AccountBalance()>=	83136	) lot=	3.04	;
if (AccountBalance()>=	83410	) lot=	3.05	;
if (AccountBalance()>=	83684	) lot=	3.06	;
if (AccountBalance()>=	83957	) lot=	3.07	;
if (AccountBalance()>=	84231	) lot=	3.08	;
if (AccountBalance()>=	84504	) lot=	3.09	;
if (AccountBalance()>=	84778	) lot=	3.10	;
if (AccountBalance()>=	85052	) lot=	3.11	;
if (AccountBalance()>=	85325	) lot=	3.12	;
if (AccountBalance()>=	85599	) lot=	3.13	;
if (AccountBalance()>=	85873	) lot=	3.14	;
if (AccountBalance()>=	86146	) lot=	3.15	;
if (AccountBalance()>=	86420	) lot=	3.16	;
if (AccountBalance()>=	86693	) lot=	3.17	;
if (AccountBalance()>=	86967	) lot=	3.18	;
if (AccountBalance()>=	87241	) lot=	3.19	;
if (AccountBalance()>=	87514	) lot=	3.20	;
if (AccountBalance()>=	87788	) lot=	3.21	;
if (AccountBalance()>=	88062	) lot=	3.22	;
if (AccountBalance()>=	88335	) lot=	3.23	;
if (AccountBalance()>=	88609	) lot=	3.24	;
if (AccountBalance()>=	88882	) lot=	3.25	;
if (AccountBalance()>=	89156	) lot=	3.26	;
if (AccountBalance()>=	89430	) lot=	3.27	;
if (AccountBalance()>=	89703	) lot=	3.28	;
if (AccountBalance()>=	89977	) lot=	3.29	;
if (AccountBalance()>=	90251	) lot=	3.30	;
if (AccountBalance()>=	90524	) lot=	3.31	;
if (AccountBalance()>=	90798	) lot=	3.32	;
if (AccountBalance()>=	91071	) lot=	3.33	;
if (AccountBalance()>=	91345	) lot=	3.34	;
if (AccountBalance()>=	91619	) lot=	3.35	;
if (AccountBalance()>=	91892	) lot=	3.36	;
if (AccountBalance()>=	92166	) lot=	3.37	;
if (AccountBalance()>=	92440	) lot=	3.38	;
if (AccountBalance()>=	92713	) lot=	3.39	;
if (AccountBalance()>=	92987	) lot=	3.40	;
if (AccountBalance()>=	93260	) lot=	3.41	;
if (AccountBalance()>=	93534	) lot=	3.42	;
if (AccountBalance()>=	93808	) lot=	3.43	;
if (AccountBalance()>=	94081	) lot=	3.44	;
if (AccountBalance()>=	94355	) lot=	3.45	;
if (AccountBalance()>=	94629	) lot=	3.46	;
if (AccountBalance()>=	94902	) lot=	3.47	;
if (AccountBalance()>=	95176	) lot=	3.48	;
if (AccountBalance()>=	95449	) lot=	3.49	;
if (AccountBalance()>=	95723	) lot=	3.50	;
if (AccountBalance()>=	95997	) lot=	3.51	;
if (AccountBalance()>=	96270	) lot=	3.52	;
if (AccountBalance()>=	96544	) lot=	3.53	;
if (AccountBalance()>=	96818	) lot=	3.54	;
if (AccountBalance()>=	97091	) lot=	3.55	;
if (AccountBalance()>=	97365	) lot=	3.56	;
if (AccountBalance()>=	97638	) lot=	3.57	;
if (AccountBalance()>=	97912	) lot=	3.58	;
if (AccountBalance()>=	98186	) lot=	3.59	;
if (AccountBalance()>=	98459	) lot=	3.60	;
if (AccountBalance()>=	98733	) lot=	3.61	;
if (AccountBalance()>=	99007	) lot=	3.62	;
if (AccountBalance()>=	99280	) lot=	3.63	;
if (AccountBalance()>=	99554	) lot=	3.64	;
if (AccountBalance()>=	99827	) lot=	3.65	;
if (AccountBalance()>=	100101	) lot=	3.66	;
if (AccountBalance()>=	100375	) lot=	3.67	;
if (AccountBalance()>=	100648	) lot=	3.68	;
if (AccountBalance()>=	100922	) lot=	3.69	;
if (AccountBalance()>=	101196	) lot=	3.70	;
if (AccountBalance()>=	101469	) lot=	3.71	;
if (AccountBalance()>=	101743	) lot=	3.72	;
if (AccountBalance()>=	102016	) lot=	3.73	;
if (AccountBalance()>=	102290	) lot=	3.74	;
if (AccountBalance()>=	102564	) lot=	3.75	;
if (AccountBalance()>=	102837	) lot=	3.76	;
if (AccountBalance()>=	103111	) lot=	3.77	;
if (AccountBalance()>=	103385	) lot=	3.78	;
if (AccountBalance()>=	103658	) lot=	3.79	;
if (AccountBalance()>=	103932	) lot=	3.80	;
if (AccountBalance()>=	104205	) lot=	3.81	;
if (AccountBalance()>=	104479	) lot=	3.82	;
if (AccountBalance()>=	104753	) lot=	3.83	;
if (AccountBalance()>=	105026	) lot=	3.84	;
if (AccountBalance()>=	105300	) lot=	3.85	;
if (AccountBalance()>=	105574	) lot=	3.86	;
if (AccountBalance()>=	105847	) lot=	3.87	;
if (AccountBalance()>=	106121	) lot=	3.88	;
if (AccountBalance()>=	106395	) lot=	3.89	;
if (AccountBalance()>=	106668	) lot=	3.90	;
if (AccountBalance()>=	106942	) lot=	3.91	;
if (AccountBalance()>=	107215	) lot=	3.92	;
if (AccountBalance()>=	107489	) lot=	3.93	;
if (AccountBalance()>=	107763	) lot=	3.94	;
if (AccountBalance()>=	108036	) lot=	3.95	;
if (AccountBalance()>=	108310	) lot=	3.96	;
if (AccountBalance()>=	108584	) lot=	3.97	;
if (AccountBalance()>=	108857	) lot=	3.98	;
if (AccountBalance()>=	109131	) lot=	3.99	;
if (AccountBalance()>=	109404	) lot=	4.00	;
if (AccountBalance()>=	109678	) lot=	4.01	;
if (AccountBalance()>=	109952	) lot=	4.02	;
if (AccountBalance()>=	110225	) lot=	4.03	;
if (AccountBalance()>=	110499	) lot=	4.04	;
if (AccountBalance()>=	110773	) lot=	4.05	;
if (AccountBalance()>=	111046	) lot=	4.06	;
if (AccountBalance()>=	111320	) lot=	4.07	;
if (AccountBalance()>=	111593	) lot=	4.08	;
if (AccountBalance()>=	111867	) lot=	4.09	;
if (AccountBalance()>=	112141	) lot=	4.10	;
if (AccountBalance()>=	112414	) lot=	4.11	;
if (AccountBalance()>=	112688	) lot=	4.12	;
if (AccountBalance()>=	112962	) lot=	4.13	;
if (AccountBalance()>=	113235	) lot=	4.14	;
if (AccountBalance()>=	113509	) lot=	4.15	;
if (AccountBalance()>=	113782	) lot=	4.16	;
if (AccountBalance()>=	114056	) lot=	4.17	;
if (AccountBalance()>=	114330	) lot=	4.18	;
if (AccountBalance()>=	114603	) lot=	4.19	;
if (AccountBalance()>=	114877	) lot=	4.20	;
if (AccountBalance()>=	115151	) lot=	4.21	;
if (AccountBalance()>=	115424	) lot=	4.22	;
if (AccountBalance()>=	115698	) lot=	4.23	;
if (AccountBalance()>=	115971	) lot=	4.24	;
if (AccountBalance()>=	116245	) lot=	4.25	;
if (AccountBalance()>=	116519	) lot=	4.26	;
if (AccountBalance()>=	116792	) lot=	4.27	;
if (AccountBalance()>=	117066	) lot=	4.28	;
if (AccountBalance()>=	117340	) lot=	4.29	;
if (AccountBalance()>=	117613	) lot=	4.30	;
if (AccountBalance()>=	117887	) lot=	4.31	;
if (AccountBalance()>=	118160	) lot=	4.32	;
if (AccountBalance()>=	118434	) lot=	4.33	;
if (AccountBalance()>=	118708	) lot=	4.34	;
if (AccountBalance()>=	118981	) lot=	4.35	;
if (AccountBalance()>=	119255	) lot=	4.36	;
if (AccountBalance()>=	119529	) lot=	4.37	;
if (AccountBalance()>=	119802	) lot=	4.38	;
if (AccountBalance()>=	120076	) lot=	4.39	;
if (AccountBalance()>=	120349	) lot=	4.40	;
if (AccountBalance()>=	120623	) lot=	4.41	;
if (AccountBalance()>=	120897	) lot=	4.42	;
if (AccountBalance()>=	121170	) lot=	4.43	;
if (AccountBalance()>=	121444	) lot=	4.44	;
if (AccountBalance()>=	121718	) lot=	4.45	;
if (AccountBalance()>=	121991	) lot=	4.46	;
if (AccountBalance()>=	122265	) lot=	4.47	;
if (AccountBalance()>=	122538	) lot=	4.48	;
if (AccountBalance()>=	122812	) lot=	4.49	;
if (AccountBalance()>=	123086	) lot=	4.50	;
if (AccountBalance()>=	123359	) lot=	4.51	;
if (AccountBalance()>=	123633	) lot=	4.52	;
if (AccountBalance()>=	123907	) lot=	4.53	;
if (AccountBalance()>=	124180	) lot=	4.54	;
if (AccountBalance()>=	124454	) lot=	4.55	;
if (AccountBalance()>=	124727	) lot=	4.56	;
if (AccountBalance()>=	125001	) lot=	4.57	;
if (AccountBalance()>=	125275	) lot=	4.58	;
if (AccountBalance()>=	125548	) lot=	4.59	;
if (AccountBalance()>=	125822	) lot=	4.60	;
if (AccountBalance()>=	126096	) lot=	4.61	;
if (AccountBalance()>=	126369	) lot=	4.62	;
if (AccountBalance()>=	126643	) lot=	4.63	;
if (AccountBalance()>=	126916	) lot=	4.64	;
if (AccountBalance()>=	127190	) lot=	4.65	;
if (AccountBalance()>=	127464	) lot=	4.66	;
if (AccountBalance()>=	127737	) lot=	4.67	;
if (AccountBalance()>=	128011	) lot=	4.68	;
if (AccountBalance()>=	128285	) lot=	4.69	;
if (AccountBalance()>=	128558	) lot=	4.70	;
if (AccountBalance()>=	128832	) lot=	4.71	;
if (AccountBalance()>=	129105	) lot=	4.72	;
if (AccountBalance()>=	129379	) lot=	4.73	;
if (AccountBalance()>=	129653	) lot=	4.74	;
if (AccountBalance()>=	129926	) lot=	4.75	;
if (AccountBalance()>=	130200	) lot=	4.76	;
if (AccountBalance()>=	130474	) lot=	4.77	;
if (AccountBalance()>=	130747	) lot=	4.78	;
if (AccountBalance()>=	131021	) lot=	4.79	;
if (AccountBalance()>=	131295	) lot=	4.80	;
if (AccountBalance()>=	131568	) lot=	4.81	;
if (AccountBalance()>=	131842	) lot=	4.82	;
if (AccountBalance()>=	132115	) lot=	4.83	;
if (AccountBalance()>=	132389	) lot=	4.84	;
if (AccountBalance()>=	132663	) lot=	4.85	;
if (AccountBalance()>=	132936	) lot=	4.86	;
if (AccountBalance()>=	133210	) lot=	4.87	;
if (AccountBalance()>=	133484	) lot=	4.88	;
if (AccountBalance()>=	133757	) lot=	4.89	;
if (AccountBalance()>=	134031	) lot=	4.90	;
if (AccountBalance()>=	134304	) lot=	4.91	;
if (AccountBalance()>=	134578	) lot=	4.92	;
if (AccountBalance()>=	134852	) lot=	4.93	;
if (AccountBalance()>=	135125	) lot=	4.94	;
if (AccountBalance()>=	135399	) lot=	4.95	;
if (AccountBalance()>=	135673	) lot=	4.96	;
if (AccountBalance()>=	135946	) lot=	4.97	;
if (AccountBalance()>=	136220	) lot=	4.98	;
if (AccountBalance()>=	136493	) lot=	4.99	;
if (AccountBalance()>=	136767	) lot=	5.00	;
if (AccountBalance()>=	137041	) lot=	5.01	;
if (AccountBalance()>=	137314	) lot=	5.02	;
if (AccountBalance()>=	137588	) lot=	5.03	;
if (AccountBalance()>=	137862	) lot=	5.04	;
if (AccountBalance()>=	138135	) lot=	5.05	;
if (AccountBalance()>=	138409	) lot=	5.06	;
if (AccountBalance()>=	138682	) lot=	5.07	;
if (AccountBalance()>=	138956	) lot=	5.08	;
if (AccountBalance()>=	139230	) lot=	5.09	;
if (AccountBalance()>=	139503	) lot=	5.10	;
if (AccountBalance()>=	139777	) lot=	5.11	;
if (AccountBalance()>=	140051	) lot=	5.12	;
if (AccountBalance()>=	140324	) lot=	5.13	;
if (AccountBalance()>=	140598	) lot=	5.14	;
if (AccountBalance()>=	140871	) lot=	5.15	;
if (AccountBalance()>=	141145	) lot=	5.16	;
if (AccountBalance()>=	141419	) lot=	5.17	;
if (AccountBalance()>=	141692	) lot=	5.18	;
if (AccountBalance()>=	141966	) lot=	5.19	;
if (AccountBalance()>=	142240	) lot=	5.20	;
if (AccountBalance()>=	142513	) lot=	5.21	;
if (AccountBalance()>=	142787	) lot=	5.22	;
if (AccountBalance()>=	143060	) lot=	5.23	;
if (AccountBalance()>=	143334	) lot=	5.24	;
if (AccountBalance()>=	143608	) lot=	5.25	;
if (AccountBalance()>=	143881	) lot=	5.26	;
if (AccountBalance()>=	144155	) lot=	5.27	;
if (AccountBalance()>=	144429	) lot=	5.28	;
if (AccountBalance()>=	144702	) lot=	5.29	;
if (AccountBalance()>=	144976	) lot=	5.30	;
if (AccountBalance()>=	145249	) lot=	5.31	;
if (AccountBalance()>=	145523	) lot=	5.32	;
if (AccountBalance()>=	145797	) lot=	5.33	;
if (AccountBalance()>=	146070	) lot=	5.34	;
if (AccountBalance()>=	146344	) lot=	5.35	;
if (AccountBalance()>=	146618	) lot=	5.36	;
if (AccountBalance()>=	146891	) lot=	5.37	;
if (AccountBalance()>=	147165	) lot=	5.38	;
if (AccountBalance()>=	147438	) lot=	5.39	;
if (AccountBalance()>=	147712	) lot=	5.40	;
if (AccountBalance()>=	147986	) lot=	5.41	;
if (AccountBalance()>=	148259	) lot=	5.42	;
if (AccountBalance()>=	148533	) lot=	5.43	;
if (AccountBalance()>=	148807	) lot=	5.44	;
if (AccountBalance()>=	149080	) lot=	5.45	;
if (AccountBalance()>=	149354	) lot=	5.46	;
if (AccountBalance()>=	149627	) lot=	5.47	;
if (AccountBalance()>=	149901	) lot=	5.48	;
if (AccountBalance()>=	150175	) lot=	5.49	;
if (AccountBalance()>=	150448	) lot=	5.50	;
if (AccountBalance()>=	150722	) lot=	5.51	;
if (AccountBalance()>=	150996	) lot=	5.52	;
if (AccountBalance()>=	151269	) lot=	5.53	;
if (AccountBalance()>=	151543	) lot=	5.54	;
if (AccountBalance()>=	151816	) lot=	5.55	;
if (AccountBalance()>=	152090	) lot=	5.56	;
if (AccountBalance()>=	152364	) lot=	5.57	;
if (AccountBalance()>=	152637	) lot=	5.58	;
if (AccountBalance()>=	152911	) lot=	5.59	;
if (AccountBalance()>=	153185	) lot=	5.60	;
if (AccountBalance()>=	153458	) lot=	5.61	;
if (AccountBalance()>=	153732	) lot=	5.62	;
if (AccountBalance()>=	154005	) lot=	5.63	;
if (AccountBalance()>=	154279	) lot=	5.64	;
if (AccountBalance()>=	154553	) lot=	5.65	;
if (AccountBalance()>=	154826	) lot=	5.66	;
if (AccountBalance()>=	155100	) lot=	5.67	;
if (AccountBalance()>=	155374	) lot=	5.68	;
if (AccountBalance()>=	155647	) lot=	5.69	;
if (AccountBalance()>=	155921	) lot=	5.70	;
if (AccountBalance()>=	156195	) lot=	5.71	;
if (AccountBalance()>=	156468	) lot=	5.72	;
if (AccountBalance()>=	156742	) lot=	5.73	;
if (AccountBalance()>=	157015	) lot=	5.74	;
if (AccountBalance()>=	157289	) lot=	5.75	;
if (AccountBalance()>=	157563	) lot=	5.76	;
if (AccountBalance()>=	157836	) lot=	5.77	;
if (AccountBalance()>=	158110	) lot=	5.78	;
if (AccountBalance()>=	158384	) lot=	5.79	;
if (AccountBalance()>=	158657	) lot=	5.80	;
if (AccountBalance()>=	158931	) lot=	5.81	;
if (AccountBalance()>=	159204	) lot=	5.82	;
if (AccountBalance()>=	159478	) lot=	5.83	;
if (AccountBalance()>=	159752	) lot=	5.84	;
if (AccountBalance()>=	160025	) lot=	5.85	;
if (AccountBalance()>=	160299	) lot=	5.86	;
if (AccountBalance()>=	160573	) lot=	5.87	;
if (AccountBalance()>=	160846	) lot=	5.88	;
if (AccountBalance()>=	161120	) lot=	5.89	;
if (AccountBalance()>=	161393	) lot=	5.90	;
if (AccountBalance()>=	161667	) lot=	5.91	;
if (AccountBalance()>=	161941	) lot=	5.92	;
if (AccountBalance()>=	162214	) lot=	5.93	;
if (AccountBalance()>=	162488	) lot=	5.94	;
if (AccountBalance()>=	162762	) lot=	5.95	;
if (AccountBalance()>=	163035	) lot=	5.96	;
if (AccountBalance()>=	163309	) lot=	5.97	;
if (AccountBalance()>=	163582	) lot=	5.98	;
if (AccountBalance()>=	163856	) lot=	5.99	;
if (AccountBalance()>=	164130	) lot=	6.00	;
if (AccountBalance()>=	164403	) lot=	6.01	;
if (AccountBalance()>=	164677	) lot=	6.02	;
if (AccountBalance()>=	164951	) lot=	6.03	;
if (AccountBalance()>=	165224	) lot=	6.04	;
if (AccountBalance()>=	165498	) lot=	6.05	;
if (AccountBalance()>=	165771	) lot=	6.06	;
if (AccountBalance()>=	166045	) lot=	6.07	;
if (AccountBalance()>=	166319	) lot=	6.08	;
if (AccountBalance()>=	166592	) lot=	6.09	;
if (AccountBalance()>=	166866	) lot=	6.10	;
if (AccountBalance()>=	167140	) lot=	6.11	;
if (AccountBalance()>=	167413	) lot=	6.12	;
if (AccountBalance()>=	167687	) lot=	6.13	;
if (AccountBalance()>=	167960	) lot=	6.14	;
if (AccountBalance()>=	168234	) lot=	6.15	;
if (AccountBalance()>=	168508	) lot=	6.16	;
if (AccountBalance()>=	168781	) lot=	6.17	;
if (AccountBalance()>=	169055	) lot=	6.18	;
if (AccountBalance()>=	169329	) lot=	6.19	;
if (AccountBalance()>=	169602	) lot=	6.20	;
if (AccountBalance()>=	169876	) lot=	6.21	;
if (AccountBalance()>=	170149	) lot=	6.22	;
if (AccountBalance()>=	170423	) lot=	6.23	;
if (AccountBalance()>=	170697	) lot=	6.24	;
if (AccountBalance()>=	170970	) lot=	6.25	;
if (AccountBalance()>=	171244	) lot=	6.26	;
if (AccountBalance()>=	171518	) lot=	6.27	;
if (AccountBalance()>=	171791	) lot=	6.28	;
if (AccountBalance()>=	172065	) lot=	6.29	;
if (AccountBalance()>=	172338	) lot=	6.30	;
if (AccountBalance()>=	172612	) lot=	6.31	;
if (AccountBalance()>=	172886	) lot=	6.32	;
if (AccountBalance()>=	173159	) lot=	6.33	;
if (AccountBalance()>=	173433	) lot=	6.34	;
if (AccountBalance()>=	173707	) lot=	6.35	;
if (AccountBalance()>=	173980	) lot=	6.36	;
if (AccountBalance()>=	174254	) lot=	6.37	;
if (AccountBalance()>=	174527	) lot=	6.38	;
if (AccountBalance()>=	174801	) lot=	6.39	;
if (AccountBalance()>=	175075	) lot=	6.40	;
if (AccountBalance()>=	175348	) lot=	6.41	;
if (AccountBalance()>=	175622	) lot=	6.42	;
if (AccountBalance()>=	175896	) lot=	6.43	;
if (AccountBalance()>=	176169	) lot=	6.44	;
if (AccountBalance()>=	176443	) lot=	6.45	;
if (AccountBalance()>=	176716	) lot=	6.46	;
if (AccountBalance()>=	176990	) lot=	6.47	;
if (AccountBalance()>=	177264	) lot=	6.48	;
if (AccountBalance()>=	177537	) lot=	6.49	;
if (AccountBalance()>=	177811	) lot=	6.50	;
if (AccountBalance()>=	178085	) lot=	6.51	;
if (AccountBalance()>=	178358	) lot=	6.52	;
if (AccountBalance()>=	178632	) lot=	6.53	;
if (AccountBalance()>=	178905	) lot=	6.54	;
if (AccountBalance()>=	179179	) lot=	6.55	;
if (AccountBalance()>=	179453	) lot=	6.56	;
if (AccountBalance()>=	179726	) lot=	6.57	;
if (AccountBalance()>=	180000	) lot=	6.58	;
if (AccountBalance()>=	180274	) lot=	6.59	;
if (AccountBalance()>=	180547	) lot=	6.60	;
if (AccountBalance()>=	180821	) lot=	6.61	;
if (AccountBalance()>=	181095	) lot=	6.62	;
if (AccountBalance()>=	181368	) lot=	6.63	;
if (AccountBalance()>=	181642	) lot=	6.64	;
if (AccountBalance()>=	181915	) lot=	6.65	;
if (AccountBalance()>=	182189	) lot=	6.66	;
if (AccountBalance()>=	182463	) lot=	6.67	;
if (AccountBalance()>=	182736	) lot=	6.68	;
if (AccountBalance()>=	183010	) lot=	6.69	;
if (AccountBalance()>=	183284	) lot=	6.70	;
if (AccountBalance()>=	183557	) lot=	6.71	;
if (AccountBalance()>=	183831	) lot=	6.72	;
if (AccountBalance()>=	184104	) lot=	6.73	;
if (AccountBalance()>=	184378	) lot=	6.74	;
if (AccountBalance()>=	184652	) lot=	6.75	;
if (AccountBalance()>=	184925	) lot=	6.76	;
if (AccountBalance()>=	185199	) lot=	6.77	;
if (AccountBalance()>=	185473	) lot=	6.78	;
if (AccountBalance()>=	185746	) lot=	6.79	;
if (AccountBalance()>=	186020	) lot=	6.80	;
if (AccountBalance()>=	186293	) lot=	6.81	;
if (AccountBalance()>=	186567	) lot=	6.82	;
if (AccountBalance()>=	186841	) lot=	6.83	;
if (AccountBalance()>=	187114	) lot=	6.84	;
if (AccountBalance()>=	187388	) lot=	6.85	;
if (AccountBalance()>=	187662	) lot=	6.86	;
if (AccountBalance()>=	187935	) lot=	6.87	;
if (AccountBalance()>=	188209	) lot=	6.88	;
if (AccountBalance()>=	188482	) lot=	6.89	;
if (AccountBalance()>=	188756	) lot=	6.90	;
if (AccountBalance()>=	189030	) lot=	6.91	;
if (AccountBalance()>=	189303	) lot=	6.92	;
if (AccountBalance()>=	189577	) lot=	6.93	;
if (AccountBalance()>=	189851	) lot=	6.94	;
if (AccountBalance()>=	190124	) lot=	6.95	;
if (AccountBalance()>=	190398	) lot=	6.96	;
if (AccountBalance()>=	190671	) lot=	6.97	;
if (AccountBalance()>=	190945	) lot=	6.98	;
if (AccountBalance()>=	191219	) lot=	6.99	;
if (AccountBalance()>=	191492	) lot=	7.00	;
if (AccountBalance()>=	191766	) lot=	7.01	;
if (AccountBalance()>=	192040	) lot=	7.02	;
if (AccountBalance()>=	192313	) lot=	7.03	;
if (AccountBalance()>=	192587	) lot=	7.04	;
if (AccountBalance()>=	192860	) lot=	7.05	;
if (AccountBalance()>=	193134	) lot=	7.06	;
if (AccountBalance()>=	193408	) lot=	7.07	;
if (AccountBalance()>=	193681	) lot=	7.08	;
if (AccountBalance()>=	193955	) lot=	7.09	;
if (AccountBalance()>=	194229	) lot=	7.10	;
if (AccountBalance()>=	194502	) lot=	7.11	;
if (AccountBalance()>=	194776	) lot=	7.12	;
if (AccountBalance()>=	195049	) lot=	7.13	;
if (AccountBalance()>=	195323	) lot=	7.14	;
if (AccountBalance()>=	195597	) lot=	7.15	;
if (AccountBalance()>=	195870	) lot=	7.16	;
if (AccountBalance()>=	196144	) lot=	7.17	;
if (AccountBalance()>=	196418	) lot=	7.18	;
if (AccountBalance()>=	196691	) lot=	7.19	;
if (AccountBalance()>=	196965	) lot=	7.20	;
if (AccountBalance()>=	197238	) lot=	7.21	;
if (AccountBalance()>=	197512	) lot=	7.22	;
if (AccountBalance()>=	197786	) lot=	7.23	;
if (AccountBalance()>=	198059	) lot=	7.24	;
if (AccountBalance()>=	198333	) lot=	7.25	;
if (AccountBalance()>=	198607	) lot=	7.26	;
if (AccountBalance()>=	198880	) lot=	7.27	;
if (AccountBalance()>=	199154	) lot=	7.28	;
if (AccountBalance()>=	199427	) lot=	7.29	;
if (AccountBalance()>=	199701	) lot=	7.30	;
if (AccountBalance()>=	199975	) lot=	7.31	;
if (AccountBalance()>=	200248	) lot=	7.32	;
if (AccountBalance()>=	200522	) lot=	7.33	;
if (AccountBalance()>=	200796	) lot=	7.34	;
if (AccountBalance()>=	201069	) lot=	7.35	;
if (AccountBalance()>=	201343	) lot=	7.36	;
if (AccountBalance()>=	201616	) lot=	7.37	;
if (AccountBalance()>=	201890	) lot=	7.38	;
if (AccountBalance()>=	202164	) lot=	7.39	;
if (AccountBalance()>=	202437	) lot=	7.40	;
if (AccountBalance()>=	202711	) lot=	7.41	;
if (AccountBalance()>=	202985	) lot=	7.42	;
if (AccountBalance()>=	203258	) lot=	7.43	;
if (AccountBalance()>=	203532	) lot=	7.44	;
if (AccountBalance()>=	203805	) lot=	7.45	;
if (AccountBalance()>=	204079	) lot=	7.46	;
if (AccountBalance()>=	204353	) lot=	7.47	;
if (AccountBalance()>=	204626	) lot=	7.48	;
if (AccountBalance()>=	204900	) lot=	7.49	;
if (AccountBalance()>=	205174	) lot=	7.50	;
if (AccountBalance()>=	205447	) lot=	7.51	;
if (AccountBalance()>=	205721	) lot=	7.52	;
if (AccountBalance()>=	205995	) lot=	7.53	;
if (AccountBalance()>=	206268	) lot=	7.54	;
if (AccountBalance()>=	206542	) lot=	7.55	;
if (AccountBalance()>=	206815	) lot=	7.56	;
if (AccountBalance()>=	207089	) lot=	7.57	;
if (AccountBalance()>=	207363	) lot=	7.58	;
if (AccountBalance()>=	207636	) lot=	7.59	;
if (AccountBalance()>=	207910	) lot=	7.60	;
if (AccountBalance()>=	208184	) lot=	7.61	;
if (AccountBalance()>=	208457	) lot=	7.62	;
if (AccountBalance()>=	208731	) lot=	7.63	;
if (AccountBalance()>=	209004	) lot=	7.64	;
if (AccountBalance()>=	209278	) lot=	7.65	;
if (AccountBalance()>=	209552	) lot=	7.66	;
if (AccountBalance()>=	209825	) lot=	7.67	;
if (AccountBalance()>=	210099	) lot=	7.68	;
if (AccountBalance()>=	210373	) lot=	7.69	;
if (AccountBalance()>=	210646	) lot=	7.70	;
if (AccountBalance()>=	210920	) lot=	7.71	;
if (AccountBalance()>=	211193	) lot=	7.72	;
if (AccountBalance()>=	211467	) lot=	7.73	;
if (AccountBalance()>=	211741	) lot=	7.74	;
if (AccountBalance()>=	212014	) lot=	7.75	;
if (AccountBalance()>=	212288	) lot=	7.76	;
if (AccountBalance()>=	212562	) lot=	7.77	;
if (AccountBalance()>=	212835	) lot=	7.78	;
if (AccountBalance()>=	213109	) lot=	7.79	;
if (AccountBalance()>=	213382	) lot=	7.80	;
if (AccountBalance()>=	213656	) lot=	7.81	;
if (AccountBalance()>=	213930	) lot=	7.82	;
if (AccountBalance()>=	214203	) lot=	7.83	;
if (AccountBalance()>=	214477	) lot=	7.84	;
if (AccountBalance()>=	214751	) lot=	7.85	;
if (AccountBalance()>=	215024	) lot=	7.86	;
if (AccountBalance()>=	215298	) lot=	7.87	;
if (AccountBalance()>=	215571	) lot=	7.88	;
if (AccountBalance()>=	215845	) lot=	7.89	;
if (AccountBalance()>=	216119	) lot=	7.90	;
if (AccountBalance()>=	216392	) lot=	7.91	;
if (AccountBalance()>=	216666	) lot=	7.92	;
if (AccountBalance()>=	216940	) lot=	7.93	;
if (AccountBalance()>=	217213	) lot=	7.94	;
if (AccountBalance()>=	217487	) lot=	7.95	;
if (AccountBalance()>=	217760	) lot=	7.96	;
if (AccountBalance()>=	218034	) lot=	7.97	;
if (AccountBalance()>=	218308	) lot=	7.98	;
if (AccountBalance()>=	218581	) lot=	7.99	;
if (AccountBalance()>=	218855	) lot=	8.00	;
if (AccountBalance()>=	219129	) lot=	8.01	;
if (AccountBalance()>=	219402	) lot=	8.02	;
if (AccountBalance()>=	219676	) lot=	8.03	;
if (AccountBalance()>=	219949	) lot=	8.04	;
if (AccountBalance()>=	220223	) lot=	8.05	;
if (AccountBalance()>=	220497	) lot=	8.06	;
if (AccountBalance()>=	220770	) lot=	8.07	;
if (AccountBalance()>=	221044	) lot=	8.08	;
if (AccountBalance()>=	221318	) lot=	8.09	;
if (AccountBalance()>=	221591	) lot=	8.10	;
if (AccountBalance()>=	221865	) lot=	8.11	;
if (AccountBalance()>=	222138	) lot=	8.12	;
if (AccountBalance()>=	222412	) lot=	8.13	;
if (AccountBalance()>=	222686	) lot=	8.14	;
if (AccountBalance()>=	222959	) lot=	8.15	;
if (AccountBalance()>=	223233	) lot=	8.16	;
if (AccountBalance()>=	223507	) lot=	8.17	;
if (AccountBalance()>=	223780	) lot=	8.18	;
if (AccountBalance()>=	224054	) lot=	8.19	;
if (AccountBalance()>=	224327	) lot=	8.20	;
if (AccountBalance()>=	224601	) lot=	8.21	;
if (AccountBalance()>=	224875	) lot=	8.22	;
if (AccountBalance()>=	225148	) lot=	8.23	;
if (AccountBalance()>=	225422	) lot=	8.24	;
if (AccountBalance()>=	225696	) lot=	8.25	;
if (AccountBalance()>=	225969	) lot=	8.26	;
if (AccountBalance()>=	226243	) lot=	8.27	;
if (AccountBalance()>=	226516	) lot=	8.28	;
if (AccountBalance()>=	226790	) lot=	8.29	;
if (AccountBalance()>=	227064	) lot=	8.30	;
if (AccountBalance()>=	227337	) lot=	8.31	;
if (AccountBalance()>=	227611	) lot=	8.32	;
if (AccountBalance()>=	227885	) lot=	8.33	;
if (AccountBalance()>=	228158	) lot=	8.34	;
if (AccountBalance()>=	228432	) lot=	8.35	;
if (AccountBalance()>=	228705	) lot=	8.36	;
if (AccountBalance()>=	228979	) lot=	8.37	;
if (AccountBalance()>=	229253	) lot=	8.38	;
if (AccountBalance()>=	229526	) lot=	8.39	;
if (AccountBalance()>=	229800	) lot=	8.40	;
if (AccountBalance()>=	230074	) lot=	8.41	;
if (AccountBalance()>=	230347	) lot=	8.42	;
if (AccountBalance()>=	230621	) lot=	8.43	;
if (AccountBalance()>=	230895	) lot=	8.44	;
if (AccountBalance()>=	231168	) lot=	8.45	;
if (AccountBalance()>=	231442	) lot=	8.46	;
if (AccountBalance()>=	231715	) lot=	8.47	;
if (AccountBalance()>=	231989	) lot=	8.48	;
if (AccountBalance()>=	232263	) lot=	8.49	;
if (AccountBalance()>=	232536	) lot=	8.50	;
if (AccountBalance()>=	232810	) lot=	8.51	;
if (AccountBalance()>=	233084	) lot=	8.52	;
if (AccountBalance()>=	233357	) lot=	8.53	;
if (AccountBalance()>=	233631	) lot=	8.54	;
if (AccountBalance()>=	233904	) lot=	8.55	;
if (AccountBalance()>=	234178	) lot=	8.56	;
if (AccountBalance()>=	234452	) lot=	8.57	;
if (AccountBalance()>=	234725	) lot=	8.58	;
if (AccountBalance()>=	234999	) lot=	8.59	;
if (AccountBalance()>=	235273	) lot=	8.60	;
if (AccountBalance()>=	235546	) lot=	8.61	;
if (AccountBalance()>=	235820	) lot=	8.62	;
if (AccountBalance()>=	236093	) lot=	8.63	;
if (AccountBalance()>=	236367	) lot=	8.64	;
if (AccountBalance()>=	236641	) lot=	8.65	;
if (AccountBalance()>=	236914	) lot=	8.66	;
if (AccountBalance()>=	237188	) lot=	8.67	;
if (AccountBalance()>=	237462	) lot=	8.68	;
if (AccountBalance()>=	237735	) lot=	8.69	;
if (AccountBalance()>=	238009	) lot=	8.70	;
if (AccountBalance()>=	238282	) lot=	8.71	;
if (AccountBalance()>=	238556	) lot=	8.72	;
if (AccountBalance()>=	238830	) lot=	8.73	;
if (AccountBalance()>=	239103	) lot=	8.74	;
if (AccountBalance()>=	239377	) lot=	8.75	;
if (AccountBalance()>=	239651	) lot=	8.76	;
if (AccountBalance()>=	239924	) lot=	8.77	;
if (AccountBalance()>=	240198	) lot=	8.78	;
if (AccountBalance()>=	240471	) lot=	8.79	;
if (AccountBalance()>=	240745	) lot=	8.80	;
if (AccountBalance()>=	241019	) lot=	8.81	;
if (AccountBalance()>=	241292	) lot=	8.82	;
if (AccountBalance()>=	241566	) lot=	8.83	;
if (AccountBalance()>=	241840	) lot=	8.84	;
if (AccountBalance()>=	242113	) lot=	8.85	;
if (AccountBalance()>=	242387	) lot=	8.86	;
if (AccountBalance()>=	242660	) lot=	8.87	;
if (AccountBalance()>=	242934	) lot=	8.88	;
if (AccountBalance()>=	243208	) lot=	8.89	;
if (AccountBalance()>=	243481	) lot=	8.90	;
if (AccountBalance()>=	243755	) lot=	8.91	;
if (AccountBalance()>=	244029	) lot=	8.92	;
if (AccountBalance()>=	244302	) lot=	8.93	;
if (AccountBalance()>=	244576	) lot=	8.94	;
if (AccountBalance()>=	244849	) lot=	8.95	;
if (AccountBalance()>=	245123	) lot=	8.96	;
if (AccountBalance()>=	245397	) lot=	8.97	;
if (AccountBalance()>=	245670	) lot=	8.98	;
if (AccountBalance()>=	245944	) lot=	8.99	;
if (AccountBalance()>=	246218	) lot=	9.00	;
if (AccountBalance()>=	246491	) lot=	9.01	;
if (AccountBalance()>=	246765	) lot=	9.02	;
if (AccountBalance()>=	247038	) lot=	9.03	;
if (AccountBalance()>=	247312	) lot=	9.04	;
if (AccountBalance()>=	247586	) lot=	9.05	;
if (AccountBalance()>=	247859	) lot=	9.06	;
if (AccountBalance()>=	248133	) lot=	9.07	;
if (AccountBalance()>=	248407	) lot=	9.08	;
if (AccountBalance()>=	248680	) lot=	9.09	;
if (AccountBalance()>=	248954	) lot=	9.10	;
if (AccountBalance()>=	249227	) lot=	9.11	;
if (AccountBalance()>=	249501	) lot=	9.12	;
if (AccountBalance()>=	249775	) lot=	9.13	;
if (AccountBalance()>=	250048	) lot=	9.14	;
if (AccountBalance()>=	250322	) lot=	9.15	;
if (AccountBalance()>=	250596	) lot=	9.16	;
if (AccountBalance()>=	250869	) lot=	9.17	;
if (AccountBalance()>=	251143	) lot=	9.18	;
if (AccountBalance()>=	251416	) lot=	9.19	;
if (AccountBalance()>=	251690	) lot=	9.20	;
if (AccountBalance()>=	251964	) lot=	9.21	;
if (AccountBalance()>=	252237	) lot=	9.22	;
if (AccountBalance()>=	252511	) lot=	9.23	;
if (AccountBalance()>=	252785	) lot=	9.24	;
if (AccountBalance()>=	253058	) lot=	9.25	;
if (AccountBalance()>=	253332	) lot=	9.26	;
if (AccountBalance()>=	253605	) lot=	9.27	;
if (AccountBalance()>=	253879	) lot=	9.28	;
if (AccountBalance()>=	254153	) lot=	9.29	;
if (AccountBalance()>=	254426	) lot=	9.30	;
if (AccountBalance()>=	254700	) lot=	9.31	;
if (AccountBalance()>=	254974	) lot=	9.32	;
if (AccountBalance()>=	255247	) lot=	9.33	;
if (AccountBalance()>=	255521	) lot=	9.34	;
if (AccountBalance()>=	255795	) lot=	9.35	;
if (AccountBalance()>=	256068	) lot=	9.36	;
if (AccountBalance()>=	256342	) lot=	9.37	;
if (AccountBalance()>=	256615	) lot=	9.38	;
if (AccountBalance()>=	256889	) lot=	9.39	;
if (AccountBalance()>=	257163	) lot=	9.40	;
if (AccountBalance()>=	257436	) lot=	9.41	;
if (AccountBalance()>=	257710	) lot=	9.42	;
if (AccountBalance()>=	257984	) lot=	9.43	;
if (AccountBalance()>=	258257	) lot=	9.44	;
if (AccountBalance()>=	258531	) lot=	9.45	;
if (AccountBalance()>=	258804	) lot=	9.46	;
if (AccountBalance()>=	259078	) lot=	9.47	;
if (AccountBalance()>=	259352	) lot=	9.48	;
if (AccountBalance()>=	259625	) lot=	9.49	;
if (AccountBalance()>=	259899	) lot=	9.50	;
if (AccountBalance()>=	260173	) lot=	9.51	;
if (AccountBalance()>=	260446	) lot=	9.52	;
if (AccountBalance()>=	260720	) lot=	9.53	;
if (AccountBalance()>=	260993	) lot=	9.54	;
if (AccountBalance()>=	261267	) lot=	9.55	;
if (AccountBalance()>=	261541	) lot=	9.56	;
if (AccountBalance()>=	261814	) lot=	9.57	;
if (AccountBalance()>=	262088	) lot=	9.58	;
if (AccountBalance()>=	262362	) lot=	9.59	;
if (AccountBalance()>=	262635	) lot=	9.60	;
if (AccountBalance()>=	262909	) lot=	9.61	;
if (AccountBalance()>=	263182	) lot=	9.62	;
if (AccountBalance()>=	263456	) lot=	9.63	;
if (AccountBalance()>=	263730	) lot=	9.64	;
if (AccountBalance()>=	264003	) lot=	9.65	;
if (AccountBalance()>=	264277	) lot=	9.66	;
if (AccountBalance()>=	264551	) lot=	9.67	;
if (AccountBalance()>=	264824	) lot=	9.68	;
if (AccountBalance()>=	265098	) lot=	9.69	;
if (AccountBalance()>=	265371	) lot=	9.70	;
if (AccountBalance()>=	265645	) lot=	9.71	;
if (AccountBalance()>=	265919	) lot=	9.72	;
if (AccountBalance()>=	266192	) lot=	9.73	;
if (AccountBalance()>=	266466	) lot=	9.74	;
if (AccountBalance()>=	266740	) lot=	9.75	;
if (AccountBalance()>=	267013	) lot=	9.76	;
if (AccountBalance()>=	267287	) lot=	9.77	;
if (AccountBalance()>=	267560	) lot=	9.78	;
if (AccountBalance()>=	267834	) lot=	9.79	;
if (AccountBalance()>=	268108	) lot=	9.80	;
if (AccountBalance()>=	268381	) lot=	9.81	;
if (AccountBalance()>=	268655	) lot=	9.82	;
if (AccountBalance()>=	268929	) lot=	9.83	;
if (AccountBalance()>=	269202	) lot=	9.84	;
if (AccountBalance()>=	269476	) lot=	9.85	;
if (AccountBalance()>=	269749	) lot=	9.86	;
if (AccountBalance()>=	270023	) lot=	9.87	;
if (AccountBalance()>=	270297	) lot=	9.88	;
if (AccountBalance()>=	270570	) lot=	9.89	;
if (AccountBalance()>=	270844	) lot=	9.90	;
if (AccountBalance()>=	271118	) lot=	9.91	;
if (AccountBalance()>=	271391	) lot=	9.92	;
if (AccountBalance()>=	271665	) lot=	9.93	;
if (AccountBalance()>=	271938	) lot=	9.94	;
if (AccountBalance()>=	272212	) lot=	9.95	;
if (AccountBalance()>=	272486	) lot=	9.96	;
if (AccountBalance()>=	272759	) lot=	9.97	;
if (AccountBalance()>=	273033	) lot=	9.98	;
if (AccountBalance()>=	273307	) lot=	9.99	;
if (AccountBalance()>=	273580	) lot=	10.00	;
}

int globPos()
// the function calculates big lot size
{
int v1=GlobalVariableGet("globalPosic");
GlobalVariableSet("globalPosic",v1+1);
  return(0);
}

int OpenLong(double volume=0.1)
// the function opens a long position with lot size=volume 
{
  int slippage=10;
  string comment="20/200 expert v2 (Long)";
  color arrow_color=Red;
  int magic=0;

    if (GlobalVariableGet("globalBalans")>AccountBalance()) volume=lot*BigLotSize;
  //  if (GlobalVariableGet("globalBalans")>AccountBalance()) if (AutoLot) LotSize();
   
  ticket=OrderSend(Symbol(),OP_BUY,volume,Ask,slippage,Ask-StopLoss_L*Point,
                      Ask+TakeProfit_L*Point,comment,magic,0,arrow_color);
 
  GlobalVariableSet("globalBalans",AccountBalance());                    
  globPos();
//  if (GlobalVariableGet("globalPosic")>25)
//  {
  GlobalVariableSet("globalPosic",0);
  if (AutoLot) LotSize();
//  }
 
  if(ticket>0)
  {
    if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
    {
      return(0);
    }
    else
      {
        Print("OpenLong(),OrderSelect() - returned an error : ",GetLastError()); 
        return(-1);
      }   
  }
  else 
  {
    Print("Error opening Buy order : ",GetLastError()); 
    return(-1);
  }
}
  
int OpenShort(double volume=0.1)
// The function opens a short position with lot size=volume
{
  int slippage=10;
  string comment="20/200 expert v2 (Short)";
  color arrow_color=Red;
  int magic=0;  

  if (GlobalVariableGet("globalBalans")>AccountBalance()) volume=lot*BigLotSize;
   
  ticket=OrderSend(Symbol(),OP_SELL,volume,Bid,slippage,Bid+StopLoss_S*Point,
                      Bid-TakeProfit_S*Point,comment,magic,0,arrow_color);
  GlobalVariableSet("globalBalans",AccountBalance());
  globPos();
//  if (GlobalVariableGet("globalPosic")>25)
//  {
  GlobalVariableSet("globalPosic",0);
  if (AutoLot) LotSize();
//  }

  if(ticket>0)
  {
    if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
      {
        return(0);
      }
    else
      {
        Print("OpenShort(),OrderSelect() - returned an error : ",GetLastError()); 
        return(-1);
      }    
  }
  else 
  {
    Print("Error opening Sell order : ",GetLastError()); 
    return(-1);
  }
}

int init()
{
  // control of a variable before using
  if (AutoLot) LotSize();
  if(!GlobalVariableCheck("globalBalans"))
    GlobalVariableSet("globalBalans",AccountBalance());
  if(!GlobalVariableCheck("globalPosic"))
    GlobalVariableSet("globalPosic",0);
  return(0);
}

int deinit()
{   
  return(0);
}

int start()
{
  if((TimeHour(TimeCurrent())>TradeTime)) cantrade=true;  
  // check if there are open orders ...
  total=OrdersTotal();
  if(total<Orders)
  {
    // ... if no open orders, go further
    // check if it's time for trade
    if((TimeHour(TimeCurrent())==TradeTime)&&(cantrade))
    {
      // ... if it is
      if (((Open[t1]-Open[t2])>delta_S*Point)) //if it is
      {
        //condition is fulfilled, enter a short position:
        // check if there is free money for opening a short position
        if(AccountFreeMarginCheck(Symbol(),OP_SELL,lot)<=0 || GetLastError()==134)
        {
          Print("Not enough money");
          return(0);
        }
        OpenShort(lot);
        
        cantrade=false; //prohibit repeated trade until the next bar
        return(0);
      }
      if (((Open[t2]-Open[t1])>delta_L*Point)) //if the price increased by delta
      {
        // condition is fulfilled, enter a long position
        // check if there is free money
        if(AccountFreeMarginCheck(Symbol(),OP_BUY,lot)<=0 || GetLastError()==134)
        {
          Print("Not enough money");
          return(0);
        }
        OpenLong(lot);
        
        cantrade=false;
        return(0);
      }
    }
  }
// block of a trade validity time checking, if MaxOpenTime=0, do not check.
   if(MaxOpenTime>0)
   {
      for(cnt=0;cnt<total;cnt++)
      {
         if (OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES))
         {
            tmp = (TimeCurrent()-OrderOpenTime())/3600.0;
               if (((NormalizeDouble(tmp,8)-MaxOpenTime)>=0))
               {     
                  RefreshRates();
                  if (OrderType()==OP_BUY)
                     closeprice=Bid;
                  else  
                     closeprice=Ask;          
                  if (OrderClose(OrderTicket(),OrderLots(),closeprice,10,Green))
                  {
                  Print("Forced closing of the trade - №",OrderTicket());
                     OrderPrint();
                  }
                  else 
                     Print("OrderClose() in block of a trade validity time checking returned an error - ",GetLastError());        
                  } 
               }
               else 
                  Print("OrderSelect() in block of a trade validity time checking returned an error - ",GetLastError());
         } 
      }     
      return(0);
   }OrderSelect() в блоке проверки времени жизни сделки вернул ошибку - ",GetLastError());
         } 
      }     
      return(0);
   }
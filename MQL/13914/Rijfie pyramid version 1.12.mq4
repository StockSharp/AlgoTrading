//+------------------------------------------------------------------+
//|                                                 mql4pyramide.mq4 |
//|                                                     DH Rijfkogel |
//|                                        donaldrijfkogel@chello.nl |
//+------------------------------------------------------------------+
#property copyright "DH Rijfkogel"
#property link      "donaldrijfkogel@chello.nl"
#property version   "1.10"
#property strict
extern color font_color=White;
extern int font_size=12;
//---
int PipAdjust,NrOfDigits;
double point;
double pips;
extern double lowlevel = 10;
extern double maxprice = 9.50;
extern double lowprice = 7.5;
extern int magicnumber = 1600;
extern int timechart =1;
extern int maperiod = 5;
extern double steplevel1 = 10;
double trailerpoints1 =MarketInfo(Symbol(),MODE_STOPLEVEL);
extern double Profit = 20;
double firstopen = Symbol()	+	"	1	";
double buyorder;
double adjustorder;



extern bool   CloseAll  = false;
 int    day       =     3;
extern int    hour      =   20;
extern int    minute    =    55;

string	test1	=	Symbol()	+	"	1	";
string	test2	=	Symbol()	+	"	2	";
string	test3	=	Symbol()	+	"	3	";
string	test4	=	Symbol()	+	"	4	";
string	test5	=	Symbol()	+	"	5	";
string	test6	=	Symbol()	+	"	6	";
string	test7	=	Symbol()	+	"	7	";
string	test8	=	Symbol()	+	"	8	";
string	test9	=	Symbol()	+	"	9	";
string	test10	=	Symbol()	+	"	10	";
string	test11	=	Symbol()	+	"	11	";
string	test12	=	Symbol()	+	"	12	";
string	test13	=	Symbol()	+	"	13	";
string	test14	=	Symbol()	+	"	14	";
string	test15	=	Symbol()	+	"	15	";
string	test16	=	Symbol()	+	"	16	";
string	test17	=	Symbol()	+	"	17	";
string	test18	=	Symbol()	+	"	18	";
string	test19	=	Symbol()	+	"	19	";
string	test20	=	Symbol()	+	"	20	";
string	test21	=	Symbol()	+	"	21	";
string	test22	=	Symbol()	+	"	22	";
string	test23	=	Symbol()	+	"	23	";
string	test24	=	Symbol()	+	"	24	";
string	test25	=	Symbol()	+	"	25	";
string	test26	=	Symbol()	+	"	26	";
string	test27	=	Symbol()	+	"	27	";
string	test28	=	Symbol()	+	"	28	";
string	test29	=	Symbol()	+	"	29	";
string	test30	=	Symbol()	+	"	30	";
string	test31	=	Symbol()	+	"	31	";
string	test32	=	Symbol()	+	"	32	";
string	test33	=	Symbol()	+	"	33	";
string	test34	=	Symbol()	+	"	34	";
string	test35	=	Symbol()	+	"	35	";
string	test36	=	Symbol()	+	"	36	";
string	test37	=	Symbol()	+	"	37	";
string	test38	=	Symbol()	+	"	38	";
string	test39	=	Symbol()	+	"	39	";
string	test40	=	Symbol()	+	"	40	";
string	test41	=	Symbol()	+	"	41	";
string	test42	=	Symbol()	+	"	42	";
string	test43	=	Symbol()	+	"	43	";
string	test44	=	Symbol()	+	"	44	";
string	test45	=	Symbol()	+	"	45	";
string	test46	=	Symbol()	+	"	46	";
string	test47	=	Symbol()	+	"	47	";
string	test48	=	Symbol()	+	"	48	";
string	test49	=	Symbol()	+	"	49	";
string	test50	=	Symbol()	+	"	50	";
string	test51	=	Symbol()	+	"	51	";
string	test52	=	Symbol()	+	"	52	";
string	test53	=	Symbol()	+	"	53	";
string	test54	=	Symbol()	+	"	54	";
string	test55	=	Symbol()	+	"	55	";
string	test56	=	Symbol()	+	"	56	";
string	test57	=	Symbol()	+	"	57	";
string	test58	=	Symbol()	+	"	58	";
string	test59	=	Symbol()	+	"	59	";
string	test60	=	Symbol()	+	"	60	";
string	test61	=	Symbol()	+	"	61	";
string	test62	=	Symbol()	+	"	62	";
string	test63	=	Symbol()	+	"	63	";
string	test64	=	Symbol()	+	"	64	";
string	test65	=	Symbol()	+	"	65	";
string	test66	=	Symbol()	+	"	66	";
string	test67	=	Symbol()	+	"	67	";
string	test68	=	Symbol()	+	"	68	";
string	test69	=	Symbol()	+	"	69	";
string	test70	=	Symbol()	+	"	70	";
string	test71	=	Symbol()	+	"	71	";
string	test72	=	Symbol()	+	"	72	";
string	test73	=	Symbol()	+	"	73	";
string	test74	=	Symbol()	+	"	74	";
string	test75	=	Symbol()	+	"	75	";
string	test76	=	Symbol()	+	"	76	";
string	test77	=	Symbol()	+	"	77	";
string	test78	=	Symbol()	+	"	78	";
string	test79	=	Symbol()	+	"	79	";
string	test80	=	Symbol()	+	"	80	";
string	test81	=	Symbol()	+	"	81	";
string	test82	=	Symbol()	+	"	82	";
string	test83	=	Symbol()	+	"	83	";
string	test84	=	Symbol()	+	"	84	";
string	test85	=	Symbol()	+	"	85	";
string	test86	=	Symbol()	+	"	86	";
string	test87	=	Symbol()	+	"	87	";
string	test88	=	Symbol()	+	"	88	";
string	test89	=	Symbol()	+	"	89	";
string	test90	=	Symbol()	+	"	90	";
string	test91	=	Symbol()	+	"	91	";
string	test92	=	Symbol()	+	"	92	";
string	test93	=	Symbol()	+	"	93	";
string	test94	=	Symbol()	+	"	94	";
string	test95	=	Symbol()	+	"	95	";
string	test96	=	Symbol()	+	"	96	";
string	test97	=	Symbol()	+	"	97	";
string	test98	=	Symbol()	+	"	98	";
string	test99	=	Symbol()	+	"	99	";
string	test100	=	Symbol()	+	"	100	";
string	test101	=	Symbol()	+	"	101	";
string	test102	=	Symbol()	+	"	102	";
string	test103	=	Symbol()	+	"	103	";
string	test104	=	Symbol()	+	"	104	";
string	test105	=	Symbol()	+	"	105	";
string	test106	=	Symbol()	+	"	106	";
string	test107	=	Symbol()	+	"	107	";
string	test108	=	Symbol()	+	"	108	";
string	test109	=	Symbol()	+	"	109	";
string	test110	=	Symbol()	+	"	110	";
string	test111	=	Symbol()	+	"	111	";
string	test112	=	Symbol()	+	"	112	";
string	test113	=	Symbol()	+	"	113	";
string	test114	=	Symbol()	+	"	114	";
string	test115	=	Symbol()	+	"	115	";
string	test116	=	Symbol()	+	"	116	";
string	test117	=	Symbol()	+	"	117	";
string	test118	=	Symbol()	+	"	118	";
string	test119	=	Symbol()	+	"	119	";
string	test120	=	Symbol()	+	"	120	";
string	test121	=	Symbol()	+	"	121	";
string	test122	=	Symbol()	+	"	122	";
string	test123	=	Symbol()	+	"	123	";
string	test124	=	Symbol()	+	"	124	";
string	test125	=	Symbol()	+	"	125	";
string	test126	=	Symbol()	+	"	126	";
string	test127	=	Symbol()	+	"	127	";
string	test128	=	Symbol()	+	"	128	";
string	test129	=	Symbol()	+	"	129	";
string	test130	=	Symbol()	+	"	130	";
string	test131	=	Symbol()	+	"	131	";
string	test132	=	Symbol()	+	"	132	";
string	test133	=	Symbol()	+	"	133	";
string	test134	=	Symbol()	+	"	134	";
string	test135	=	Symbol()	+	"	135	";
string	test136	=	Symbol()	+	"	136	";
string	test137	=	Symbol()	+	"	137	";
string	test138	=	Symbol()	+	"	138	";
string	test139	=	Symbol()	+	"	139	";
string	test140	=	Symbol()	+	"	140	";
string	test141	=	Symbol()	+	"	141	";
string	test142	=	Symbol()	+	"	142	";
string	test143	=	Symbol()	+	"	143	";
string	test144	=	Symbol()	+	"	144	";
string	test145	=	Symbol()	+	"	145	";
string	test146	=	Symbol()	+	"	146	";
string	test147	=	Symbol()	+	"	147	";
string	test148	=	Symbol()	+	"	148	";
string	test149	=	Symbol()	+	"	149	";
string	test150	=	Symbol()	+	"	150	";
string	test151	=	Symbol()	+	"	151	";
string	test152	=	Symbol()	+	"	152	";
string	test153	=	Symbol()	+	"	153	";
string	test154	=	Symbol()	+	"	154	";
string	test155	=	Symbol()	+	"	155	";
string	test156	=	Symbol()	+	"	156	";
string	test157	=	Symbol()	+	"	157	";
string	test158	=	Symbol()	+	"	158	";
string	test159	=	Symbol()	+	"	159	";
string	test160	=	Symbol()	+	"	160	";
string	test161	=	Symbol()	+	"	161	";
string	test162	=	Symbol()	+	"	162	";
string	test163	=	Symbol()	+	"	163	";
string	test164	=	Symbol()	+	"	164	";
string	test165	=	Symbol()	+	"	165	";
string	test166	=	Symbol()	+	"	166	";
string	test167	=	Symbol()	+	"	167	";
string	test168	=	Symbol()	+	"	168	";
string	test169	=	Symbol()	+	"	169	";
string	test170	=	Symbol()	+	"	170	";
string	test171	=	Symbol()	+	"	171	";
string	test172	=	Symbol()	+	"	172	";
string	test173	=	Symbol()	+	"	173	";
string	test174	=	Symbol()	+	"	174	";
string	test175	=	Symbol()	+	"	175	";
string	test176	=	Symbol()	+	"	176	";
string	test177	=	Symbol()	+	"	177	";
string	test178	=	Symbol()	+	"	178	";
string	test179	=	Symbol()	+	"	179	";
string	test180	=	Symbol()	+	"	180	";
string	test181	=	Symbol()	+	"	181	";
string	test182	=	Symbol()	+	"	182	";
string	test183	=	Symbol()	+	"	183	";
string	test184	=	Symbol()	+	"	184	";
string	test185	=	Symbol()	+	"	185	";
string	test186	=	Symbol()	+	"	186	";
string	test187	=	Symbol()	+	"	187	";
string	test188	=	Symbol()	+	"	188	";
string	test189	=	Symbol()	+	"	189	";
string	test190	=	Symbol()	+	"	190	";
string	test191	=	Symbol()	+	"	191	";
string	test192	=	Symbol()	+	"	192	";
string	test193	=	Symbol()	+	"	193	";
string	test194	=	Symbol()	+	"	194	";
string	test195	=	Symbol()	+	"	195	";
string	test196	=	Symbol()	+	"	196	";
string	test197	=	Symbol()	+	"	197	";
string	test198	=	Symbol()	+	"	198	";
string	test199	=	Symbol()	+	"	199	";
string	test200	=	Symbol()	+	"	200	";
string	test201	=	Symbol()	+	"	201	";
string	test202	=	Symbol()	+	"	202	";
string	test203	=	Symbol()	+	"	203	";
string	test204	=	Symbol()	+	"	204	";
string	test205	=	Symbol()	+	"	205	";
string	test206	=	Symbol()	+	"	206	";
string	test207	=	Symbol()	+	"	207	";
string	test208	=	Symbol()	+	"	208	";
string	test209	=	Symbol()	+	"	209	";
string	test210	=	Symbol()	+	"	210	";
string	test211	=	Symbol()	+	"	211	";
string	test212	=	Symbol()	+	"	212	";
string	test213	=	Symbol()	+	"	213	";
string	test214	=	Symbol()	+	"	214	";
string	test215	=	Symbol()	+	"	215	";
string	test216	=	Symbol()	+	"	216	";
string	test217	=	Symbol()	+	"	217	";
string	test218	=	Symbol()	+	"	218	";
string	test219	=	Symbol()	+	"	219	";
string	test220	=	Symbol()	+	"	220	";
string	test221	=	Symbol()	+	"	221	";
string	test222	=	Symbol()	+	"	222	";
string	test223	=	Symbol()	+	"	223	";
string	test224	=	Symbol()	+	"	224	";
string	test225	=	Symbol()	+	"	225	";
string	test226	=	Symbol()	+	"	226	";
string	test227	=	Symbol()	+	"	227	";
string	test228	=	Symbol()	+	"	228	";
string	test229	=	Symbol()	+	"	229	";
string	test230	=	Symbol()	+	"	230	";
string	test231	=	Symbol()	+	"	231	";
string	test232	=	Symbol()	+	"	232	";
string	test233	=	Symbol()	+	"	233	";
string	test234	=	Symbol()	+	"	234	";
string	test235	=	Symbol()	+	"	235	";
string	test236	=	Symbol()	+	"	236	";
string	test237	=	Symbol()	+	"	237	";
string	test238	=	Symbol()	+	"	238	";
string	test239	=	Symbol()	+	"	239	";
string	test240	=	Symbol()	+	"	240	";
string	test241	=	Symbol()	+	"	241	";
string	test242	=	Symbol()	+	"	242	";
string	test243	=	Symbol()	+	"	243	";
string	test244	=	Symbol()	+	"	244	";
string	test245	=	Symbol()	+	"	245	";
string	test246	=	Symbol()	+	"	246	";
string	test247	=	Symbol()	+	"	247	";
string	test248	=	Symbol()	+	"	248	";
string	test249	=	Symbol()	+	"	249	";
string	test250	=	Symbol()	+	"	250	";
string	test251	=	Symbol()	+	"	251	";
string	test252	=	Symbol()	+	"	252	";
string	test253	=	Symbol()	+	"	253	";
string	test254	=	Symbol()	+	"	254	";
string	test255	=	Symbol()	+	"	255	";
string	test256	=	Symbol()	+	"	256	";
string	test257	=	Symbol()	+	"	257	";
string	test258	=	Symbol()	+	"	258	";
string	test259	=	Symbol()	+	"	259	";
string	test260	=	Symbol()	+	"	260	";
string	test261	=	Symbol()	+	"	261	";
string	test262	=	Symbol()	+	"	262	";
string	test263	=	Symbol()	+	"	263	";
string	test264	=	Symbol()	+	"	264	";
string	test265	=	Symbol()	+	"	265	";
string	test266	=	Symbol()	+	"	266	";
string	test267	=	Symbol()	+	"	267	";
string	test268	=	Symbol()	+	"	268	";
string	test269	=	Symbol()	+	"	269	";
string	test270	=	Symbol()	+	"	270	";
string	test271	=	Symbol()	+	"	271	";
string	test272	=	Symbol()	+	"	272	";
string	test273	=	Symbol()	+	"	273	";
string	test274	=	Symbol()	+	"	274	";
string	test275	=	Symbol()	+	"	275	";
string	test276	=	Symbol()	+	"	276	";
string	test277	=	Symbol()	+	"	277	";
string	test278	=	Symbol()	+	"	278	";
string	test279	=	Symbol()	+	"	279	";
string	test280	=	Symbol()	+	"	280	";
string	test281	=	Symbol()	+	"	281	";
string	test282	=	Symbol()	+	"	282	";
string	test283	=	Symbol()	+	"	283	";
string	test284	=	Symbol()	+	"	284	";
string	test285	=	Symbol()	+	"	285	";
string	test286	=	Symbol()	+	"	286	";
string	test287	=	Symbol()	+	"	287	";
string	test288	=	Symbol()	+	"	288	";
string	test289	=	Symbol()	+	"	289	";
string	test290	=	Symbol()	+	"	290	";
string	test291	=	Symbol()	+	"	291	";
string	test292	=	Symbol()	+	"	292	";
string	test293	=	Symbol()	+	"	293	";
string	test294	=	Symbol()	+	"	294	";
string	test295	=	Symbol()	+	"	295	";
string	test296	=	Symbol()	+	"	296	";
string	test297	=	Symbol()	+	"	297	";
string	test298	=	Symbol()	+	"	298	";
string	test299	=	Symbol()	+	"	299	";
string	test300	=	Symbol()	+	"	300	";
string	test301	=	Symbol()	+	"	301	";
string	test302	=	Symbol()	+	"	302	";
string	test303	=	Symbol()	+	"	303	";
string	test304	=	Symbol()	+	"	304	";
string	test305	=	Symbol()	+	"	305	";
string	test306	=	Symbol()	+	"	306	";
string	test307	=	Symbol()	+	"	307	";
string	test308	=	Symbol()	+	"	308	";
string	test309	=	Symbol()	+	"	309	";
string	test310	=	Symbol()	+	"	310	";
string	test311	=	Symbol()	+	"	311	";
string	test312	=	Symbol()	+	"	312	";
string	test313	=	Symbol()	+	"	313	";
string	test314	=	Symbol()	+	"	314	";
string	test315	=	Symbol()	+	"	315	";
string	test316	=	Symbol()	+	"	316	";
string	test317	=	Symbol()	+	"	317	";
string	test318	=	Symbol()	+	"	318	";
string	test319	=	Symbol()	+	"	319	";
string	test320	=	Symbol()	+	"	320	";
string	test321	=	Symbol()	+	"	321	";
string	test322	=	Symbol()	+	"	322	";
string	test323	=	Symbol()	+	"	323	";
string	test324	=	Symbol()	+	"	324	";
string	test325	=	Symbol()	+	"	325	";
string	test326	=	Symbol()	+	"	326	";
string	test327	=	Symbol()	+	"	327	";
string	test328	=	Symbol()	+	"	328	";
string	test329	=	Symbol()	+	"	329	";
string	test330	=	Symbol()	+	"	330	";
string	test331	=	Symbol()	+	"	331	";
string	test332	=	Symbol()	+	"	332	";
string	test333	=	Symbol()	+	"	333	";
string	test334	=	Symbol()	+	"	334	";
string	test335	=	Symbol()	+	"	335	";
string	test336	=	Symbol()	+	"	336	";
string	test337	=	Symbol()	+	"	337	";
string	test338	=	Symbol()	+	"	338	";
string	test339	=	Symbol()	+	"	339	";
string	test340	=	Symbol()	+	"	340	";
string	test341	=	Symbol()	+	"	341	";
string	test342	=	Symbol()	+	"	342	";
string	test343	=	Symbol()	+	"	343	";
string	test344	=	Symbol()	+	"	344	";
string	test345	=	Symbol()	+	"	345	";
string	test346	=	Symbol()	+	"	346	";
string	test347	=	Symbol()	+	"	347	";
string	test348	=	Symbol()	+	"	348	";
string	test349	=	Symbol()	+	"	349	";
string	test350	=	Symbol()	+	"	350	";
string	test351	=	Symbol()	+	"	351	";
string	test352	=	Symbol()	+	"	352	";
string	test353	=	Symbol()	+	"	353	";
string	test354	=	Symbol()	+	"	354	";
string	test355	=	Symbol()	+	"	355	";
string	test356	=	Symbol()	+	"	356	";
string	test357	=	Symbol()	+	"	357	";
string	test358	=	Symbol()	+	"	358	";
string	test359	=	Symbol()	+	"	359	";
string	test360	=	Symbol()	+	"	360	";
string	test361	=	Symbol()	+	"	361	";
string	test362	=	Symbol()	+	"	362	";
string	test363	=	Symbol()	+	"	363	";
string	test364	=	Symbol()	+	"	364	";
string	test365	=	Symbol()	+	"	365	";
string	test366	=	Symbol()	+	"	366	";
string	test367	=	Symbol()	+	"	367	";
string	test368	=	Symbol()	+	"	368	";
string	test369	=	Symbol()	+	"	369	";
string	test370	=	Symbol()	+	"	370	";
string	test371	=	Symbol()	+	"	371	";
string	test372	=	Symbol()	+	"	372	";
string	test373	=	Symbol()	+	"	373	";
string	test374	=	Symbol()	+	"	374	";
string	test375	=	Symbol()	+	"	375	";
string	test376	=	Symbol()	+	"	376	";
string	test377	=	Symbol()	+	"	377	";
string	test378	=	Symbol()	+	"	378	";
string	test379	=	Symbol()	+	"	379	";
string	test380	=	Symbol()	+	"	380	";
string	test381	=	Symbol()	+	"	381	";
string	test382	=	Symbol()	+	"	382	";
string	test383	=	Symbol()	+	"	383	";
string	test384	=	Symbol()	+	"	384	";
string	test385	=	Symbol()	+	"	385	";
string	test386	=	Symbol()	+	"	386	";
string	test387	=	Symbol()	+	"	387	";
string	test388	=	Symbol()	+	"	388	";
string	test389	=	Symbol()	+	"	389	";
string	test390	=	Symbol()	+	"	390	";
string	test391	=	Symbol()	+	"	391	";
string	test392	=	Symbol()	+	"	392	";
string	test393	=	Symbol()	+	"	393	";
string	test394	=	Symbol()	+	"	394	";
string	test395	=	Symbol()	+	"	395	";
string	test396	=	Symbol()	+	"	396	";
string	test397	=	Symbol()	+	"	397	";
string	test398	=	Symbol()	+	"	398	";
string	test399	=	Symbol()	+	"	399	";
string	test400	=	Symbol()	+	"	400	";
string	test401	=	Symbol()	+	"	401	";
string	test402	=	Symbol()	+	"	402	";
string	test403	=	Symbol()	+	"	403	";
string	test404	=	Symbol()	+	"	404	";
string	test405	=	Symbol()	+	"	405	";
string	test406	=	Symbol()	+	"	406	";
string	test407	=	Symbol()	+	"	407	";
string	test408	=	Symbol()	+	"	408	";
string	test409	=	Symbol()	+	"	409	";
string	test410	=	Symbol()	+	"	410	";
string	test411	=	Symbol()	+	"	411	";
string	test412	=	Symbol()	+	"	412	";
string	test413	=	Symbol()	+	"	413	";
string	test414	=	Symbol()	+	"	414	";
string	test415	=	Symbol()	+	"	415	";
string	test416	=	Symbol()	+	"	416	";
string	test417	=	Symbol()	+	"	417	";
string	test418	=	Symbol()	+	"	418	";
string	test419	=	Symbol()	+	"	419	";
string	test420	=	Symbol()	+	"	420	";
string	test421	=	Symbol()	+	"	421	";
string	test422	=	Symbol()	+	"	422	";
string	test423	=	Symbol()	+	"	423	";
string	test424	=	Symbol()	+	"	424	";
string	test425	=	Symbol()	+	"	425	";
string	test426	=	Symbol()	+	"	426	";
string	test427	=	Symbol()	+	"	427	";
string	test428	=	Symbol()	+	"	428	";
string	test429	=	Symbol()	+	"	429	";
string	test430	=	Symbol()	+	"	430	";
string	test431	=	Symbol()	+	"	431	";
string	test432	=	Symbol()	+	"	432	";
string	test433	=	Symbol()	+	"	433	";
string	test434	=	Symbol()	+	"	434	";
string	test435	=	Symbol()	+	"	435	";
string	test436	=	Symbol()	+	"	436	";
string	test437	=	Symbol()	+	"	437	";
string	test438	=	Symbol()	+	"	438	";
string	test439	=	Symbol()	+	"	439	";
string	test440	=	Symbol()	+	"	440	";
string	test441	=	Symbol()	+	"	441	";
string	test442	=	Symbol()	+	"	442	";
string	test443	=	Symbol()	+	"	443	";
string	test444	=	Symbol()	+	"	444	";
string	test445	=	Symbol()	+	"	445	";
string	test446	=	Symbol()	+	"	446	";
string	test447	=	Symbol()	+	"	447	";
string	test448	=	Symbol()	+	"	448	";
string	test449	=	Symbol()	+	"	449	";
string	test450	=	Symbol()	+	"	450	";
string	test451	=	Symbol()	+	"	451	";
string	test452	=	Symbol()	+	"	452	";
string	test453	=	Symbol()	+	"	453	";
string	test454	=	Symbol()	+	"	454	";
string	test455	=	Symbol()	+	"	455	";
string	test456	=	Symbol()	+	"	456	";
string	test457	=	Symbol()	+	"	457	";
string	test458	=	Symbol()	+	"	458	";
string	test459	=	Symbol()	+	"	459	";
string	test460	=	Symbol()	+	"	460	";
string	test461	=	Symbol()	+	"	461	";
string	test462	=	Symbol()	+	"	462	";
string	test463	=	Symbol()	+	"	463	";
string	test464	=	Symbol()	+	"	464	";
string	test465	=	Symbol()	+	"	465	";
string	test466	=	Symbol()	+	"	466	";
string	test467	=	Symbol()	+	"	467	";
string	test468	=	Symbol()	+	"	468	";
string	test469	=	Symbol()	+	"	469	";
string	test470	=	Symbol()	+	"	470	";
string	test471	=	Symbol()	+	"	471	";
string	test472	=	Symbol()	+	"	472	";
string	test473	=	Symbol()	+	"	473	";
string	test474	=	Symbol()	+	"	474	";
string	test475	=	Symbol()	+	"	475	";
string	test476	=	Symbol()	+	"	476	";
string	test477	=	Symbol()	+	"	477	";
string	test478	=	Symbol()	+	"	478	";
string	test479	=	Symbol()	+	"	479	";
string	test480	=	Symbol()	+	"	480	";
string	test481	=	Symbol()	+	"	481	";
string	test482	=	Symbol()	+	"	482	";
string	test483	=	Symbol()	+	"	483	";
string	test484	=	Symbol()	+	"	484	";
string	test485	=	Symbol()	+	"	485	";
string	test486	=	Symbol()	+	"	486	";
string	test487	=	Symbol()	+	"	487	";
string	test488	=	Symbol()	+	"	488	";
string	test489	=	Symbol()	+	"	489	";
string	test490	=	Symbol()	+	"	490	";
string	test491	=	Symbol()	+	"	491	";
string	test492	=	Symbol()	+	"	492	";
string	test493	=	Symbol()	+	"	493	";
string	test494	=	Symbol()	+	"	494	";
string	test495	=	Symbol()	+	"	495	";
string	test496	=	Symbol()	+	"	496	";
string	test497	=	Symbol()	+	"	497	";
string	test498	=	Symbol()	+	"	498	";
string	test499	=	Symbol()	+	"	499	";
string	test500	=	Symbol()	+	"	500	";
string	test501	=	Symbol()	+	"	501	";
string	test502	=	Symbol()	+	"	502	";
string	test503	=	Symbol()	+	"	503	";
string	test504	=	Symbol()	+	"	504	";
string	test505	=	Symbol()	+	"	505	";
string	test506	=	Symbol()	+	"	506	";
string	test507	=	Symbol()	+	"	507	";
string	test508	=	Symbol()	+	"	508	";
string	test509	=	Symbol()	+	"	509	";
string	test510	=	Symbol()	+	"	510	";
string	test511	=	Symbol()	+	"	511	";
string	test512	=	Symbol()	+	"	512	";
string	test513	=	Symbol()	+	"	513	";
string	test514	=	Symbol()	+	"	514	";
string	test515	=	Symbol()	+	"	515	";
string	test516	=	Symbol()	+	"	516	";
string	test517	=	Symbol()	+	"	517	";
string	test518	=	Symbol()	+	"	518	";
string	test519	=	Symbol()	+	"	519	";
string	test520	=	Symbol()	+	"	520	";
string	test521	=	Symbol()	+	"	521	";
string	test522	=	Symbol()	+	"	522	";
string	test523	=	Symbol()	+	"	523	";
string	test524	=	Symbol()	+	"	524	";
string	test525	=	Symbol()	+	"	525	";
string	test526	=	Symbol()	+	"	526	";
string	test527	=	Symbol()	+	"	527	";
string	test528	=	Symbol()	+	"	528	";
string	test529	=	Symbol()	+	"	529	";
string	test530	=	Symbol()	+	"	530	";
string	test531	=	Symbol()	+	"	531	";
string	test532	=	Symbol()	+	"	532	";
string	test533	=	Symbol()	+	"	533	";
string	test534	=	Symbol()	+	"	534	";
string	test535	=	Symbol()	+	"	535	";
string	test536	=	Symbol()	+	"	536	";
string	test537	=	Symbol()	+	"	537	";
string	test538	=	Symbol()	+	"	538	";
string	test539	=	Symbol()	+	"	539	";
string	test540	=	Symbol()	+	"	540	";
string	test541	=	Symbol()	+	"	541	";
string	test542	=	Symbol()	+	"	542	";
string	test543	=	Symbol()	+	"	543	";
string	test544	=	Symbol()	+	"	544	";
string	test545	=	Symbol()	+	"	545	";
string	test546	=	Symbol()	+	"	546	";
string	test547	=	Symbol()	+	"	547	";
string	test548	=	Symbol()	+	"	548	";
string	test549	=	Symbol()	+	"	549	";
string	test550	=	Symbol()	+	"	550	";
string	test551	=	Symbol()	+	"	551	";
string	test552	=	Symbol()	+	"	552	";
string	test553	=	Symbol()	+	"	553	";
string	test554	=	Symbol()	+	"	554	";
string	test555	=	Symbol()	+	"	555	";
string	test556	=	Symbol()	+	"	556	";
string	test557	=	Symbol()	+	"	557	";
string	test558	=	Symbol()	+	"	558	";
string	test559	=	Symbol()	+	"	559	";
string	test560	=	Symbol()	+	"	560	";
string	test561	=	Symbol()	+	"	561	";
string	test562	=	Symbol()	+	"	562	";
string	test563	=	Symbol()	+	"	563	";
string	test564	=	Symbol()	+	"	564	";
string	test565	=	Symbol()	+	"	565	";
string	test566	=	Symbol()	+	"	566	";
string	test567	=	Symbol()	+	"	567	";
string	test568	=	Symbol()	+	"	568	";
string	test569	=	Symbol()	+	"	569	";
string	test570	=	Symbol()	+	"	570	";
string	test571	=	Symbol()	+	"	571	";
string	test572	=	Symbol()	+	"	572	";
string	test573	=	Symbol()	+	"	573	";
string	test574	=	Symbol()	+	"	574	";
string	test575	=	Symbol()	+	"	575	";
string	test576	=	Symbol()	+	"	576	";
string	test577	=	Symbol()	+	"	577	";
string	test578	=	Symbol()	+	"	578	";
string	test579	=	Symbol()	+	"	579	";
string	test580	=	Symbol()	+	"	580	";
string	test581	=	Symbol()	+	"	581	";
string	test582	=	Symbol()	+	"	582	";
string	test583	=	Symbol()	+	"	583	";
string	test584	=	Symbol()	+	"	584	";
string	test585	=	Symbol()	+	"	585	";
string	test586	=	Symbol()	+	"	586	";
string	test587	=	Symbol()	+	"	587	";
string	test588	=	Symbol()	+	"	588	";
string	test589	=	Symbol()	+	"	589	";
string	test590	=	Symbol()	+	"	590	";
string	test591	=	Symbol()	+	"	591	";
string	test592	=	Symbol()	+	"	592	";
string	test593	=	Symbol()	+	"	593	";
string	test594	=	Symbol()	+	"	594	";
string	test595	=	Symbol()	+	"	595	";
string	test596	=	Symbol()	+	"	596	";
string	test597	=	Symbol()	+	"	597	";
string	test598	=	Symbol()	+	"	598	";
string	test599	=	Symbol()	+	"	599	";
string	test600	=	Symbol()	+	"	600	";
string	test601	=	Symbol()	+	"	601	";
string	test602	=	Symbol()	+	"	602	";
string	test603	=	Symbol()	+	"	603	";
string	test604	=	Symbol()	+	"	604	";
string	test605	=	Symbol()	+	"	605	";
string	test606	=	Symbol()	+	"	606	";
string	test607	=	Symbol()	+	"	607	";
string	test608	=	Symbol()	+	"	608	";
string	test609	=	Symbol()	+	"	609	";
string	test610	=	Symbol()	+	"	610	";
string	test611	=	Symbol()	+	"	611	";
string	test612	=	Symbol()	+	"	612	";
string	test613	=	Symbol()	+	"	613	";
string	test614	=	Symbol()	+	"	614	";
string	test615	=	Symbol()	+	"	615	";
string	test616	=	Symbol()	+	"	616	";
string	test617	=	Symbol()	+	"	617	";
string	test618	=	Symbol()	+	"	618	";
string	test619	=	Symbol()	+	"	619	";
string	test620	=	Symbol()	+	"	620	";
string	test621	=	Symbol()	+	"	621	";
string	test622	=	Symbol()	+	"	622	";
string	test623	=	Symbol()	+	"	623	";
string	test624	=	Symbol()	+	"	624	";
string	test625	=	Symbol()	+	"	625	";
string	test626	=	Symbol()	+	"	626	";
string	test627	=	Symbol()	+	"	627	";
string	test628	=	Symbol()	+	"	628	";
string	test629	=	Symbol()	+	"	629	";
string	test630	=	Symbol()	+	"	630	";
string	test631	=	Symbol()	+	"	631	";
string	test632	=	Symbol()	+	"	632	";
string	test633	=	Symbol()	+	"	633	";
string	test634	=	Symbol()	+	"	634	";
string	test635	=	Symbol()	+	"	635	";
string	test636	=	Symbol()	+	"	636	";
string	test637	=	Symbol()	+	"	637	";
string	test638	=	Symbol()	+	"	638	";
string	test639	=	Symbol()	+	"	639	";
string	test640	=	Symbol()	+	"	640	";
string	test641	=	Symbol()	+	"	641	";
string	test642	=	Symbol()	+	"	642	";
string	test643	=	Symbol()	+	"	643	";
string	test644	=	Symbol()	+	"	644	";
string	test645	=	Symbol()	+	"	645	";
string	test646	=	Symbol()	+	"	646	";
string	test647	=	Symbol()	+	"	647	";
string	test648	=	Symbol()	+	"	648	";
string	test649	=	Symbol()	+	"	649	";
string	test650	=	Symbol()	+	"	650	";
string	test651	=	Symbol()	+	"	651	";
string	test652	=	Symbol()	+	"	652	";
string	test653	=	Symbol()	+	"	653	";
string	test654	=	Symbol()	+	"	654	";
string	test655	=	Symbol()	+	"	655	";
string	test656	=	Symbol()	+	"	656	";
string	test657	=	Symbol()	+	"	657	";
string	test658	=	Symbol()	+	"	658	";
string	test659	=	Symbol()	+	"	659	";
string	test660	=	Symbol()	+	"	660	";
string	test661	=	Symbol()	+	"	661	";
string	test662	=	Symbol()	+	"	662	";
string	test663	=	Symbol()	+	"	663	";
string	test664	=	Symbol()	+	"	664	";
string	test665	=	Symbol()	+	"	665	";
string	test666	=	Symbol()	+	"	666	";
string	test667	=	Symbol()	+	"	667	";
string	test668	=	Symbol()	+	"	668	";
string	test669	=	Symbol()	+	"	669	";
string	test670	=	Symbol()	+	"	670	";
string	test671	=	Symbol()	+	"	671	";
string	test672	=	Symbol()	+	"	672	";
string	test673	=	Symbol()	+	"	673	";
string	test674	=	Symbol()	+	"	674	";
string	test675	=	Symbol()	+	"	675	";
string	test676	=	Symbol()	+	"	676	";
string	test677	=	Symbol()	+	"	677	";
string	test678	=	Symbol()	+	"	678	";
string	test679	=	Symbol()	+	"	679	";
string	test680	=	Symbol()	+	"	680	";
string	test681	=	Symbol()	+	"	681	";
string	test682	=	Symbol()	+	"	682	";
string	test683	=	Symbol()	+	"	683	";
string	test684	=	Symbol()	+	"	684	";
string	test685	=	Symbol()	+	"	685	";
string	test686	=	Symbol()	+	"	686	";
string	test687	=	Symbol()	+	"	687	";
string	test688	=	Symbol()	+	"	688	";
string	test689	=	Symbol()	+	"	689	";
string	test690	=	Symbol()	+	"	690	";
string	test691	=	Symbol()	+	"	691	";
string	test692	=	Symbol()	+	"	692	";
string	test693	=	Symbol()	+	"	693	";
string	test694	=	Symbol()	+	"	694	";
string	test695	=	Symbol()	+	"	695	";
string	test696	=	Symbol()	+	"	696	";
string	test697	=	Symbol()	+	"	697	";
string	test698	=	Symbol()	+	"	698	";
string	test699	=	Symbol()	+	"	699	";
string	test700	=	Symbol()	+	"	700	";
string	test701	=	Symbol()	+	"	701	";
string	test702	=	Symbol()	+	"	702	";
string	test703	=	Symbol()	+	"	703	";
string	test704	=	Symbol()	+	"	704	";
string	test705	=	Symbol()	+	"	705	";
string	test706	=	Symbol()	+	"	706	";
string	test707	=	Symbol()	+	"	707	";
string	test708	=	Symbol()	+	"	708	";
string	test709	=	Symbol()	+	"	709	";
string	test710	=	Symbol()	+	"	710	";
string	test711	=	Symbol()	+	"	711	";
string	test712	=	Symbol()	+	"	712	";
string	test713	=	Symbol()	+	"	713	";
string	test714	=	Symbol()	+	"	714	";
string	test715	=	Symbol()	+	"	715	";
string	test716	=	Symbol()	+	"	716	";
string	test717	=	Symbol()	+	"	717	";
string	test718	=	Symbol()	+	"	718	";
string	test719	=	Symbol()	+	"	719	";
string	test720	=	Symbol()	+	"	720	";
string	test721	=	Symbol()	+	"	721	";
string	test722	=	Symbol()	+	"	722	";
string	test723	=	Symbol()	+	"	723	";
string	test724	=	Symbol()	+	"	724	";
string	test725	=	Symbol()	+	"	725	";
string	test726	=	Symbol()	+	"	726	";
string	test727	=	Symbol()	+	"	727	";
string	test728	=	Symbol()	+	"	728	";
string	test729	=	Symbol()	+	"	729	";
string	test730	=	Symbol()	+	"	730	";
string	test731	=	Symbol()	+	"	731	";
string	test732	=	Symbol()	+	"	732	";
string	test733	=	Symbol()	+	"	733	";
string	test734	=	Symbol()	+	"	734	";
string	test735	=	Symbol()	+	"	735	";
string	test736	=	Symbol()	+	"	736	";
string	test737	=	Symbol()	+	"	737	";
string	test738	=	Symbol()	+	"	738	";
string	test739	=	Symbol()	+	"	739	";
string	test740	=	Symbol()	+	"	740	";
string	test741	=	Symbol()	+	"	741	";
string	test742	=	Symbol()	+	"	742	";
string	test743	=	Symbol()	+	"	743	";
string	test744	=	Symbol()	+	"	744	";
string	test745	=	Symbol()	+	"	745	";
string	test746	=	Symbol()	+	"	746	";
string	test747	=	Symbol()	+	"	747	";
string	test748	=	Symbol()	+	"	748	";
string	test749	=	Symbol()	+	"	749	";
string	test750	=	Symbol()	+	"	750	";
string	test751	=	Symbol()	+	"	751	";
string	test752	=	Symbol()	+	"	752	";
string	test753	=	Symbol()	+	"	753	";
string	test754	=	Symbol()	+	"	754	";
string	test755	=	Symbol()	+	"	755	";
string	test756	=	Symbol()	+	"	756	";
string	test757	=	Symbol()	+	"	757	";
string	test758	=	Symbol()	+	"	758	";
string	test759	=	Symbol()	+	"	759	";
string	test760	=	Symbol()	+	"	760	";
string	test761	=	Symbol()	+	"	761	";
string	test762	=	Symbol()	+	"	762	";
string	test763	=	Symbol()	+	"	763	";
string	test764	=	Symbol()	+	"	764	";
string	test765	=	Symbol()	+	"	765	";
string	test766	=	Symbol()	+	"	766	";
string	test767	=	Symbol()	+	"	767	";
string	test768	=	Symbol()	+	"	768	";
string	test769	=	Symbol()	+	"	769	";
string	test770	=	Symbol()	+	"	770	";
string	test771	=	Symbol()	+	"	771	";
string	test772	=	Symbol()	+	"	772	";
string	test773	=	Symbol()	+	"	773	";
string	test774	=	Symbol()	+	"	774	";
string	test775	=	Symbol()	+	"	775	";
string	test776	=	Symbol()	+	"	776	";
string	test777	=	Symbol()	+	"	777	";
string	test778	=	Symbol()	+	"	778	";
string	test779	=	Symbol()	+	"	779	";
string	test780	=	Symbol()	+	"	780	";
string	test781	=	Symbol()	+	"	781	";
string	test782	=	Symbol()	+	"	782	";
string	test783	=	Symbol()	+	"	783	";
string	test784	=	Symbol()	+	"	784	";
string	test785	=	Symbol()	+	"	785	";
string	test786	=	Symbol()	+	"	786	";
string	test787	=	Symbol()	+	"	787	";
string	test788	=	Symbol()	+	"	788	";
string	test789	=	Symbol()	+	"	789	";
string	test790	=	Symbol()	+	"	790	";
string	test791	=	Symbol()	+	"	791	";
string	test792	=	Symbol()	+	"	792	";
string	test793	=	Symbol()	+	"	793	";
string	test794	=	Symbol()	+	"	794	";
string	test795	=	Symbol()	+	"	795	";
string	test796	=	Symbol()	+	"	796	";
string	test797	=	Symbol()	+	"	797	";
string	test798	=	Symbol()	+	"	798	";
string	test799	=	Symbol()	+	"	799	";
string	test800	=	Symbol()	+	"	800	";
string	test801	=	Symbol()	+	"	801	";
string	test802	=	Symbol()	+	"	802	";
string	test803	=	Symbol()	+	"	803	";
string	test804	=	Symbol()	+	"	804	";
string	test805	=	Symbol()	+	"	805	";
string	test806	=	Symbol()	+	"	806	";
string	test807	=	Symbol()	+	"	807	";
string	test808	=	Symbol()	+	"	808	";
string	test809	=	Symbol()	+	"	809	";
string	test810	=	Symbol()	+	"	810	";
string	test811	=	Symbol()	+	"	811	";
string	test812	=	Symbol()	+	"	812	";
string	test813	=	Symbol()	+	"	813	";
string	test814	=	Symbol()	+	"	814	";
string	test815	=	Symbol()	+	"	815	";
string	test816	=	Symbol()	+	"	816	";
string	test817	=	Symbol()	+	"	817	";
string	test818	=	Symbol()	+	"	818	";
string	test819	=	Symbol()	+	"	819	";
string	test820	=	Symbol()	+	"	820	";
string	test821	=	Symbol()	+	"	821	";
string	test822	=	Symbol()	+	"	822	";
string	test823	=	Symbol()	+	"	823	";
string	test824	=	Symbol()	+	"	824	";
string	test825	=	Symbol()	+	"	825	";
string	test826	=	Symbol()	+	"	826	";
string	test827	=	Symbol()	+	"	827	";
string	test828	=	Symbol()	+	"	828	";
string	test829	=	Symbol()	+	"	829	";
string	test830	=	Symbol()	+	"	830	";
string	test831	=	Symbol()	+	"	831	";
string	test832	=	Symbol()	+	"	832	";
string	test833	=	Symbol()	+	"	833	";
string	test834	=	Symbol()	+	"	834	";
string	test835	=	Symbol()	+	"	835	";
string	test836	=	Symbol()	+	"	836	";
string	test837	=	Symbol()	+	"	837	";
string	test838	=	Symbol()	+	"	838	";
string	test839	=	Symbol()	+	"	839	";
string	test840	=	Symbol()	+	"	840	";
string	test841	=	Symbol()	+	"	841	";
string	test842	=	Symbol()	+	"	842	";
string	test843	=	Symbol()	+	"	843	";
string	test844	=	Symbol()	+	"	844	";
string	test845	=	Symbol()	+	"	845	";
string	test846	=	Symbol()	+	"	846	";
string	test847	=	Symbol()	+	"	847	";
string	test848	=	Symbol()	+	"	848	";
string	test849	=	Symbol()	+	"	849	";
string	test850	=	Symbol()	+	"	850	";
string	test851	=	Symbol()	+	"	851	";
string	test852	=	Symbol()	+	"	852	";
string	test853	=	Symbol()	+	"	853	";
string	test854	=	Symbol()	+	"	854	";
string	test855	=	Symbol()	+	"	855	";
string	test856	=	Symbol()	+	"	856	";
string	test857	=	Symbol()	+	"	857	";
string	test858	=	Symbol()	+	"	858	";
string	test859	=	Symbol()	+	"	859	";
string	test860	=	Symbol()	+	"	860	";
string	test861	=	Symbol()	+	"	861	";
string	test862	=	Symbol()	+	"	862	";
string	test863	=	Symbol()	+	"	863	";
string	test864	=	Symbol()	+	"	864	";
string	test865	=	Symbol()	+	"	865	";
string	test866	=	Symbol()	+	"	866	";
string	test867	=	Symbol()	+	"	867	";
string	test868	=	Symbol()	+	"	868	";
string	test869	=	Symbol()	+	"	869	";
string	test870	=	Symbol()	+	"	870	";
string	test871	=	Symbol()	+	"	871	";
string	test872	=	Symbol()	+	"	872	";
string	test873	=	Symbol()	+	"	873	";
string	test874	=	Symbol()	+	"	874	";
string	test875	=	Symbol()	+	"	875	";
string	test876	=	Symbol()	+	"	876	";
string	test877	=	Symbol()	+	"	877	";
string	test878	=	Symbol()	+	"	878	";
string	test879	=	Symbol()	+	"	879	";
string	test880	=	Symbol()	+	"	880	";
string	test881	=	Symbol()	+	"	881	";
string	test882	=	Symbol()	+	"	882	";
string	test883	=	Symbol()	+	"	883	";
string	test884	=	Symbol()	+	"	884	";
string	test885	=	Symbol()	+	"	885	";
string	test886	=	Symbol()	+	"	886	";
string	test887	=	Symbol()	+	"	887	";
string	test888	=	Symbol()	+	"	888	";
string	test889	=	Symbol()	+	"	889	";
string	test890	=	Symbol()	+	"	890	";
string	test891	=	Symbol()	+	"	891	";
string	test892	=	Symbol()	+	"	892	";
string	test893	=	Symbol()	+	"	893	";
string	test894	=	Symbol()	+	"	894	";
string	test895	=	Symbol()	+	"	895	";
string	test896	=	Symbol()	+	"	896	";
string	test897	=	Symbol()	+	"	897	";
string	test898	=	Symbol()	+	"	898	";
string	test899	=	Symbol()	+	"	899	";
string	test900	=	Symbol()	+	"	900	";
string	test901	=	Symbol()	+	"	901	";
string	test902	=	Symbol()	+	"	902	";
string	test903	=	Symbol()	+	"	903	";
string	test904	=	Symbol()	+	"	904	";
string	test905	=	Symbol()	+	"	905	";
string	test906	=	Symbol()	+	"	906	";
string	test907	=	Symbol()	+	"	907	";
string	test908	=	Symbol()	+	"	908	";
string	test909	=	Symbol()	+	"	909	";
string	test910	=	Symbol()	+	"	910	";
string	test911	=	Symbol()	+	"	911	";
string	test912	=	Symbol()	+	"	912	";
string	test913	=	Symbol()	+	"	913	";
string	test914	=	Symbol()	+	"	914	";
string	test915	=	Symbol()	+	"	915	";
string	test916	=	Symbol()	+	"	916	";
string	test917	=	Symbol()	+	"	917	";
string	test918	=	Symbol()	+	"	918	";
string	test919	=	Symbol()	+	"	919	";
string	test920	=	Symbol()	+	"	920	";
string	test921	=	Symbol()	+	"	921	";
string	test922	=	Symbol()	+	"	922	";
string	test923	=	Symbol()	+	"	923	";
string	test924	=	Symbol()	+	"	924	";
string	test925	=	Symbol()	+	"	925	";
string	test926	=	Symbol()	+	"	926	";
string	test927	=	Symbol()	+	"	927	";
string	test928	=	Symbol()	+	"	928	";
string	test929	=	Symbol()	+	"	929	";
string	test930	=	Symbol()	+	"	930	";
string	test931	=	Symbol()	+	"	931	";
string	test932	=	Symbol()	+	"	932	";
string	test933	=	Symbol()	+	"	933	";
string	test934	=	Symbol()	+	"	934	";
string	test935	=	Symbol()	+	"	935	";
string	test936	=	Symbol()	+	"	936	";
string	test937	=	Symbol()	+	"	937	";
string	test938	=	Symbol()	+	"	938	";
string	test939	=	Symbol()	+	"	939	";
string	test940	=	Symbol()	+	"	940	";
string	test941	=	Symbol()	+	"	941	";
string	test942	=	Symbol()	+	"	942	";
string	test943	=	Symbol()	+	"	943	";
string	test944	=	Symbol()	+	"	944	";
string	test945	=	Symbol()	+	"	945	";
string	test946	=	Symbol()	+	"	946	";
string	test947	=	Symbol()	+	"	947	";
string	test948	=	Symbol()	+	"	948	";
string	test949	=	Symbol()	+	"	949	";
string	test950	=	Symbol()	+	"	950	";
string	test951	=	Symbol()	+	"	951	";
string	test952	=	Symbol()	+	"	952	";
string	test953	=	Symbol()	+	"	953	";
string	test954	=	Symbol()	+	"	954	";
string	test955	=	Symbol()	+	"	955	";
string	test956	=	Symbol()	+	"	956	";
string	test957	=	Symbol()	+	"	957	";
string	test958	=	Symbol()	+	"	958	";
string	test959	=	Symbol()	+	"	959	";
string	test960	=	Symbol()	+	"	960	";
string	test961	=	Symbol()	+	"	961	";
string	test962	=	Symbol()	+	"	962	";
string	test963	=	Symbol()	+	"	963	";
string	test964	=	Symbol()	+	"	964	";
string	test965	=	Symbol()	+	"	965	";
string	test966	=	Symbol()	+	"	966	";
string	test967	=	Symbol()	+	"	967	";
string	test968	=	Symbol()	+	"	968	";
string	test969	=	Symbol()	+	"	969	";
string	test970	=	Symbol()	+	"	970	";
string	test971	=	Symbol()	+	"	971	";
string	test972	=	Symbol()	+	"	972	";
string	test973	=	Symbol()	+	"	973	";
string	test974	=	Symbol()	+	"	974	";
string	test975	=	Symbol()	+	"	975	";
string	test976	=	Symbol()	+	"	976	";
string	test977	=	Symbol()	+	"	977	";
string	test978	=	Symbol()	+	"	978	";
string	test979	=	Symbol()	+	"	979	";
string	test980	=	Symbol()	+	"	980	";
string	test981	=	Symbol()	+	"	981	";
string	test982	=	Symbol()	+	"	982	";
string	test983	=	Symbol()	+	"	983	";
string	test984	=	Symbol()	+	"	984	";
string	test985	=	Symbol()	+	"	985	";
string	test986	=	Symbol()	+	"	986	";
string	test987	=	Symbol()	+	"	987	";
string	test988	=	Symbol()	+	"	988	";
string	test989	=	Symbol()	+	"	989	";
string	test990	=	Symbol()	+	"	990	";
string	test991	=	Symbol()	+	"	991	";
string	test992	=	Symbol()	+	"	992	";
string	test993	=	Symbol()	+	"	993	";
string	test994	=	Symbol()	+	"	994	";
string	test995	=	Symbol()	+	"	995	";
string	test996	=	Symbol()	+	"	996	";
string	test997	=	Symbol()	+	"	997	";
string	test998	=	Symbol()	+	"	998	";
string	test999	=	Symbol()	+	"	999	";
string	test1000	=	Symbol()	+	"	1000	";


double	static	buy1	;
double	static	buy2	;
double	static	buy3	;
double	static	buy4	;
double	static	buy5	;
double	static	buy6	;
double	static	buy7	;
double	static	buy8	;
double	static	buy9	;
double	static	buy10	;
double	static	buy11	;
double	static	buy12	;
double	static	buy13	;
double	static	buy14	;
double	static	buy15	;
double	static	buy16	;
double	static	buy17	;
double	static	buy18	;
double	static	buy19	;
double	static	buy20	;
double	static	buy21	;
double	static	buy22	;
double	static	buy23	;
double	static	buy24	;
double	static	buy25	;
double	static	buy26	;
double	static	buy27	;
double	static	buy28	;
double	static	buy29	;
double	static	buy30	;
double	static	buy31	;
double	static	buy32	;
double	static	buy33	;
double	static	buy34	;
double	static	buy35	;
double	static	buy36	;
double	static	buy37	;
double	static	buy38	;
double	static	buy39	;
double	static	buy40	;
double	static	buy41	;
double	static	buy42	;
double	static	buy43	;
double	static	buy44	;
double	static	buy45	;
double	static	buy46	;
double	static	buy47	;
double	static	buy48	;
double	static	buy49	;
double	static	buy50	;
double	static	buy51	;
double	static	buy52	;
double	static	buy53	;
double	static	buy54	;
double	static	buy55	;
double	static	buy56	;
double	static	buy57	;
double	static	buy58	;
double	static	buy59	;
double	static	buy60	;
double	static	buy61	;
double	static	buy62	;
double	static	buy63	;
double	static	buy64	;
double	static	buy65	;
double	static	buy66	;
double	static	buy67	;
double	static	buy68	;
double	static	buy69	;
double	static	buy70	;
double	static	buy71	;
double	static	buy72	;
double	static	buy73	;
double	static	buy74	;
double	static	buy75	;
double	static	buy76	;
double	static	buy77	;
double	static	buy78	;
double	static	buy79	;
double	static	buy80	;
double	static	buy81	;
double	static	buy82	;
double	static	buy83	;
double	static	buy84	;
double	static	buy85	;
double	static	buy86	;
double	static	buy87	;
double	static	buy88	;
double	static	buy89	;
double	static	buy90	;
double	static	buy91	;
double	static	buy92	;
double	static	buy93	;
double	static	buy94	;
double	static	buy95	;
double	static	buy96	;
double	static	buy97	;
double	static	buy98	;
double	static	buy99	;
double	static	buy100	;
double	static	buy101	;
double	static	buy102	;
double	static	buy103	;
double	static	buy104	;
double	static	buy105	;
double	static	buy106	;
double	static	buy107	;
double	static	buy108	;
double	static	buy109	;
double	static	buy110	;
double	static	buy111	;
double	static	buy112	;
double	static	buy113	;
double	static	buy114	;
double	static	buy115	;
double	static	buy116	;
double	static	buy117	;
double	static	buy118	;
double	static	buy119	;
double	static	buy120	;
double	static	buy121	;
double	static	buy122	;
double	static	buy123	;
double	static	buy124	;
double	static	buy125	;
double	static	buy126	;
double	static	buy127	;
double	static	buy128	;
double	static	buy129	;
double	static	buy130	;
double	static	buy131	;
double	static	buy132	;
double	static	buy133	;
double	static	buy134	;
double	static	buy135	;
double	static	buy136	;
double	static	buy137	;
double	static	buy138	;
double	static	buy139	;
double	static	buy140	;
double	static	buy141	;
double	static	buy142	;
double	static	buy143	;
double	static	buy144	;
double	static	buy145	;
double	static	buy146	;
double	static	buy147	;
double	static	buy148	;
double	static	buy149	;
double	static	buy150	;
double	static	buy151	;
double	static	buy152	;
double	static	buy153	;
double	static	buy154	;
double	static	buy155	;
double	static	buy156	;
double	static	buy157	;
double	static	buy158	;
double	static	buy159	;
double	static	buy160	;
double	static	buy161	;
double	static	buy162	;
double	static	buy163	;
double	static	buy164	;
double	static	buy165	;
double	static	buy166	;
double	static	buy167	;
double	static	buy168	;
double	static	buy169	;
double	static	buy170	;
double	static	buy171	;
double	static	buy172	;
double	static	buy173	;
double	static	buy174	;
double	static	buy175	;
double	static	buy176	;
double	static	buy177	;
double	static	buy178	;
double	static	buy179	;
double	static	buy180	;
double	static	buy181	;
double	static	buy182	;
double	static	buy183	;
double	static	buy184	;
double	static	buy185	;
double	static	buy186	;
double	static	buy187	;
double	static	buy188	;
double	static	buy189	;
double	static	buy190	;
double	static	buy191	;
double	static	buy192	;
double	static	buy193	;
double	static	buy194	;
double	static	buy195	;
double	static	buy196	;
double	static	buy197	;
double	static	buy198	;
double	static	buy199	;
double	static	buy200	;
double	static	buy201	;
double	static	buy202	;
double	static	buy203	;
double	static	buy204	;
double	static	buy205	;
double	static	buy206	;
double	static	buy207	;
double	static	buy208	;
double	static	buy209	;
double	static	buy210	;
double	static	buy211	;
double	static	buy212	;
double	static	buy213	;
double	static	buy214	;
double	static	buy215	;
double	static	buy216	;
double	static	buy217	;
double	static	buy218	;
double	static	buy219	;
double	static	buy220	;
double	static	buy221	;
double	static	buy222	;
double	static	buy223	;
double	static	buy224	;
double	static	buy225	;
double	static	buy226	;
double	static	buy227	;
double	static	buy228	;
double	static	buy229	;
double	static	buy230	;
double	static	buy231	;
double	static	buy232	;
double	static	buy233	;
double	static	buy234	;
double	static	buy235	;
double	static	buy236	;
double	static	buy237	;
double	static	buy238	;
double	static	buy239	;
double	static	buy240	;
double	static	buy241	;
double	static	buy242	;
double	static	buy243	;
double	static	buy244	;
double	static	buy245	;
double	static	buy246	;
double	static	buy247	;
double	static	buy248	;
double	static	buy249	;
double	static	buy250	;
double	static	buy251	;
double	static	buy252	;
double	static	buy253	;
double	static	buy254	;
double	static	buy255	;
double	static	buy256	;
double	static	buy257	;
double	static	buy258	;
double	static	buy259	;
double	static	buy260	;
double	static	buy261	;
double	static	buy262	;
double	static	buy263	;
double	static	buy264	;
double	static	buy265	;
double	static	buy266	;
double	static	buy267	;
double	static	buy268	;
double	static	buy269	;
double	static	buy270	;
double	static	buy271	;
double	static	buy272	;
double	static	buy273	;
double	static	buy274	;
double	static	buy275	;
double	static	buy276	;
double	static	buy277	;
double	static	buy278	;
double	static	buy279	;
double	static	buy280	;
double	static	buy281	;
double	static	buy282	;
double	static	buy283	;
double	static	buy284	;
double	static	buy285	;
double	static	buy286	;
double	static	buy287	;
double	static	buy288	;
double	static	buy289	;
double	static	buy290	;
double	static	buy291	;
double	static	buy292	;
double	static	buy293	;
double	static	buy294	;
double	static	buy295	;
double	static	buy296	;
double	static	buy297	;
double	static	buy298	;
double	static	buy299	;
double	static	buy300	;
double	static	buy301	;
double	static	buy302	;
double	static	buy303	;
double	static	buy304	;
double	static	buy305	;
double	static	buy306	;
double	static	buy307	;
double	static	buy308	;
double	static	buy309	;
double	static	buy310	;
double	static	buy311	;
double	static	buy312	;
double	static	buy313	;
double	static	buy314	;
double	static	buy315	;
double	static	buy316	;
double	static	buy317	;
double	static	buy318	;
double	static	buy319	;
double	static	buy320	;
double	static	buy321	;
double	static	buy322	;
double	static	buy323	;
double	static	buy324	;
double	static	buy325	;
double	static	buy326	;
double	static	buy327	;
double	static	buy328	;
double	static	buy329	;
double	static	buy330	;
double	static	buy331	;
double	static	buy332	;
double	static	buy333	;
double	static	buy334	;
double	static	buy335	;
double	static	buy336	;
double	static	buy337	;
double	static	buy338	;
double	static	buy339	;
double	static	buy340	;
double	static	buy341	;
double	static	buy342	;
double	static	buy343	;
double	static	buy344	;
double	static	buy345	;
double	static	buy346	;
double	static	buy347	;
double	static	buy348	;
double	static	buy349	;
double	static	buy350	;
double	static	buy351	;
double	static	buy352	;
double	static	buy353	;
double	static	buy354	;
double	static	buy355	;
double	static	buy356	;
double	static	buy357	;
double	static	buy358	;
double	static	buy359	;
double	static	buy360	;
double	static	buy361	;
double	static	buy362	;
double	static	buy363	;
double	static	buy364	;
double	static	buy365	;
double	static	buy366	;
double	static	buy367	;
double	static	buy368	;
double	static	buy369	;
double	static	buy370	;
double	static	buy371	;
double	static	buy372	;
double	static	buy373	;
double	static	buy374	;
double	static	buy375	;
double	static	buy376	;
double	static	buy377	;
double	static	buy378	;
double	static	buy379	;
double	static	buy380	;
double	static	buy381	;
double	static	buy382	;
double	static	buy383	;
double	static	buy384	;
double	static	buy385	;
double	static	buy386	;
double	static	buy387	;
double	static	buy388	;
double	static	buy389	;
double	static	buy390	;
double	static	buy391	;
double	static	buy392	;
double	static	buy393	;
double	static	buy394	;
double	static	buy395	;
double	static	buy396	;
double	static	buy397	;
double	static	buy398	;
double	static	buy399	;
double	static	buy400	;
double	static	buy401	;
double	static	buy402	;
double	static	buy403	;
double	static	buy404	;
double	static	buy405	;
double	static	buy406	;
double	static	buy407	;
double	static	buy408	;
double	static	buy409	;
double	static	buy410	;
double	static	buy411	;
double	static	buy412	;
double	static	buy413	;
double	static	buy414	;
double	static	buy415	;
double	static	buy416	;
double	static	buy417	;
double	static	buy418	;
double	static	buy419	;
double	static	buy420	;
double	static	buy421	;
double	static	buy422	;
double	static	buy423	;
double	static	buy424	;
double	static	buy425	;
double	static	buy426	;
double	static	buy427	;
double	static	buy428	;
double	static	buy429	;
double	static	buy430	;
double	static	buy431	;
double	static	buy432	;
double	static	buy433	;
double	static	buy434	;
double	static	buy435	;
double	static	buy436	;
double	static	buy437	;
double	static	buy438	;
double	static	buy439	;
double	static	buy440	;
double	static	buy441	;
double	static	buy442	;
double	static	buy443	;
double	static	buy444	;
double	static	buy445	;
double	static	buy446	;
double	static	buy447	;
double	static	buy448	;
double	static	buy449	;
double	static	buy450	;
double	static	buy451	;
double	static	buy452	;
double	static	buy453	;
double	static	buy454	;
double	static	buy455	;
double	static	buy456	;
double	static	buy457	;
double	static	buy458	;
double	static	buy459	;
double	static	buy460	;
double	static	buy461	;
double	static	buy462	;
double	static	buy463	;
double	static	buy464	;
double	static	buy465	;
double	static	buy466	;
double	static	buy467	;
double	static	buy468	;
double	static	buy469	;
double	static	buy470	;
double	static	buy471	;
double	static	buy472	;
double	static	buy473	;
double	static	buy474	;
double	static	buy475	;
double	static	buy476	;
double	static	buy477	;
double	static	buy478	;
double	static	buy479	;
double	static	buy480	;
double	static	buy481	;
double	static	buy482	;
double	static	buy483	;
double	static	buy484	;
double	static	buy485	;
double	static	buy486	;
double	static	buy487	;
double	static	buy488	;
double	static	buy489	;
double	static	buy490	;
double	static	buy491	;
double	static	buy492	;
double	static	buy493	;
double	static	buy494	;
double	static	buy495	;
double	static	buy496	;
double	static	buy497	;
double	static	buy498	;
double	static	buy499	;
double	static	buy500	;
double	static	buy501	;
double	static	buy502	;
double	static	buy503	;
double	static	buy504	;
double	static	buy505	;
double	static	buy506	;
double	static	buy507	;
double	static	buy508	;
double	static	buy509	;
double	static	buy510	;
double	static	buy511	;
double	static	buy512	;
double	static	buy513	;
double	static	buy514	;
double	static	buy515	;
double	static	buy516	;
double	static	buy517	;
double	static	buy518	;
double	static	buy519	;
double	static	buy520	;
double	static	buy521	;
double	static	buy522	;
double	static	buy523	;
double	static	buy524	;
double	static	buy525	;
double	static	buy526	;
double	static	buy527	;
double	static	buy528	;
double	static	buy529	;
double	static	buy530	;
double	static	buy531	;
double	static	buy532	;
double	static	buy533	;
double	static	buy534	;
double	static	buy535	;
double	static	buy536	;
double	static	buy537	;
double	static	buy538	;
double	static	buy539	;
double	static	buy540	;
double	static	buy541	;
double	static	buy542	;
double	static	buy543	;
double	static	buy544	;
double	static	buy545	;
double	static	buy546	;
double	static	buy547	;
double	static	buy548	;
double	static	buy549	;
double	static	buy550	;
double	static	buy551	;
double	static	buy552	;
double	static	buy553	;
double	static	buy554	;
double	static	buy555	;
double	static	buy556	;
double	static	buy557	;
double	static	buy558	;
double	static	buy559	;
double	static	buy560	;
double	static	buy561	;
double	static	buy562	;
double	static	buy563	;
double	static	buy564	;
double	static	buy565	;
double	static	buy566	;
double	static	buy567	;
double	static	buy568	;
double	static	buy569	;
double	static	buy570	;
double	static	buy571	;
double	static	buy572	;
double	static	buy573	;
double	static	buy574	;
double	static	buy575	;
double	static	buy576	;
double	static	buy577	;
double	static	buy578	;
double	static	buy579	;
double	static	buy580	;
double	static	buy581	;
double	static	buy582	;
double	static	buy583	;
double	static	buy584	;
double	static	buy585	;
double	static	buy586	;
double	static	buy587	;
double	static	buy588	;
double	static	buy589	;
double	static	buy590	;
double	static	buy591	;
double	static	buy592	;
double	static	buy593	;
double	static	buy594	;
double	static	buy595	;
double	static	buy596	;
double	static	buy597	;
double	static	buy598	;
double	static	buy599	;
double	static	buy600	;
double	static	buy601	;
double	static	buy602	;
double	static	buy603	;
double	static	buy604	;
double	static	buy605	;
double	static	buy606	;
double	static	buy607	;
double	static	buy608	;
double	static	buy609	;
double	static	buy610	;
double	static	buy611	;
double	static	buy612	;
double	static	buy613	;
double	static	buy614	;
double	static	buy615	;
double	static	buy616	;
double	static	buy617	;
double	static	buy618	;
double	static	buy619	;
double	static	buy620	;
double	static	buy621	;
double	static	buy622	;
double	static	buy623	;
double	static	buy624	;
double	static	buy625	;
double	static	buy626	;
double	static	buy627	;
double	static	buy628	;
double	static	buy629	;
double	static	buy630	;
double	static	buy631	;
double	static	buy632	;
double	static	buy633	;
double	static	buy634	;
double	static	buy635	;
double	static	buy636	;
double	static	buy637	;
double	static	buy638	;
double	static	buy639	;
double	static	buy640	;
double	static	buy641	;
double	static	buy642	;
double	static	buy643	;
double	static	buy644	;
double	static	buy645	;
double	static	buy646	;
double	static	buy647	;
double	static	buy648	;
double	static	buy649	;
double	static	buy650	;
double	static	buy651	;
double	static	buy652	;
double	static	buy653	;
double	static	buy654	;
double	static	buy655	;
double	static	buy656	;
double	static	buy657	;
double	static	buy658	;
double	static	buy659	;
double	static	buy660	;
double	static	buy661	;
double	static	buy662	;
double	static	buy663	;
double	static	buy664	;
double	static	buy665	;
double	static	buy666	;
double	static	buy667	;
double	static	buy668	;
double	static	buy669	;
double	static	buy670	;
double	static	buy671	;
double	static	buy672	;
double	static	buy673	;
double	static	buy674	;
double	static	buy675	;
double	static	buy676	;
double	static	buy677	;
double	static	buy678	;
double	static	buy679	;
double	static	buy680	;
double	static	buy681	;
double	static	buy682	;
double	static	buy683	;
double	static	buy684	;
double	static	buy685	;
double	static	buy686	;
double	static	buy687	;
double	static	buy688	;
double	static	buy689	;
double	static	buy690	;
double	static	buy691	;
double	static	buy692	;
double	static	buy693	;
double	static	buy694	;
double	static	buy695	;
double	static	buy696	;
double	static	buy697	;
double	static	buy698	;
double	static	buy699	;
double	static	buy700	;
double	static	buy701	;
double	static	buy702	;
double	static	buy703	;
double	static	buy704	;
double	static	buy705	;
double	static	buy706	;
double	static	buy707	;
double	static	buy708	;
double	static	buy709	;
double	static	buy710	;
double	static	buy711	;
double	static	buy712	;
double	static	buy713	;
double	static	buy714	;
double	static	buy715	;
double	static	buy716	;
double	static	buy717	;
double	static	buy718	;
double	static	buy719	;
double	static	buy720	;
double	static	buy721	;
double	static	buy722	;
double	static	buy723	;
double	static	buy724	;
double	static	buy725	;
double	static	buy726	;
double	static	buy727	;
double	static	buy728	;
double	static	buy729	;
double	static	buy730	;
double	static	buy731	;
double	static	buy732	;
double	static	buy733	;
double	static	buy734	;
double	static	buy735	;
double	static	buy736	;
double	static	buy737	;
double	static	buy738	;
double	static	buy739	;
double	static	buy740	;
double	static	buy741	;
double	static	buy742	;
double	static	buy743	;
double	static	buy744	;
double	static	buy745	;
double	static	buy746	;
double	static	buy747	;
double	static	buy748	;
double	static	buy749	;
double	static	buy750	;
double	static	buy751	;
double	static	buy752	;
double	static	buy753	;
double	static	buy754	;
double	static	buy755	;
double	static	buy756	;
double	static	buy757	;
double	static	buy758	;
double	static	buy759	;
double	static	buy760	;
double	static	buy761	;
double	static	buy762	;
double	static	buy763	;
double	static	buy764	;
double	static	buy765	;
double	static	buy766	;
double	static	buy767	;
double	static	buy768	;
double	static	buy769	;
double	static	buy770	;
double	static	buy771	;
double	static	buy772	;
double	static	buy773	;
double	static	buy774	;
double	static	buy775	;
double	static	buy776	;
double	static	buy777	;
double	static	buy778	;
double	static	buy779	;
double	static	buy780	;
double	static	buy781	;
double	static	buy782	;
double	static	buy783	;
double	static	buy784	;
double	static	buy785	;
double	static	buy786	;
double	static	buy787	;
double	static	buy788	;
double	static	buy789	;
double	static	buy790	;
double	static	buy791	;
double	static	buy792	;
double	static	buy793	;
double	static	buy794	;
double	static	buy795	;
double	static	buy796	;
double	static	buy797	;
double	static	buy798	;
double	static	buy799	;
double	static	buy800	;
double	static	buy801	;
double	static	buy802	;
double	static	buy803	;
double	static	buy804	;
double	static	buy805	;
double	static	buy806	;
double	static	buy807	;
double	static	buy808	;
double	static	buy809	;
double	static	buy810	;
double	static	buy811	;
double	static	buy812	;
double	static	buy813	;
double	static	buy814	;
double	static	buy815	;
double	static	buy816	;
double	static	buy817	;
double	static	buy818	;
double	static	buy819	;
double	static	buy820	;
double	static	buy821	;
double	static	buy822	;
double	static	buy823	;
double	static	buy824	;
double	static	buy825	;
double	static	buy826	;
double	static	buy827	;
double	static	buy828	;
double	static	buy829	;
double	static	buy830	;
double	static	buy831	;
double	static	buy832	;
double	static	buy833	;
double	static	buy834	;
double	static	buy835	;
double	static	buy836	;
double	static	buy837	;
double	static	buy838	;
double	static	buy839	;
double	static	buy840	;
double	static	buy841	;
double	static	buy842	;
double	static	buy843	;
double	static	buy844	;
double	static	buy845	;
double	static	buy846	;
double	static	buy847	;
double	static	buy848	;
double	static	buy849	;
double	static	buy850	;
double	static	buy851	;
double	static	buy852	;
double	static	buy853	;
double	static	buy854	;
double	static	buy855	;
double	static	buy856	;
double	static	buy857	;
double	static	buy858	;
double	static	buy859	;
double	static	buy860	;
double	static	buy861	;
double	static	buy862	;
double	static	buy863	;
double	static	buy864	;
double	static	buy865	;
double	static	buy866	;
double	static	buy867	;
double	static	buy868	;
double	static	buy869	;
double	static	buy870	;
double	static	buy871	;
double	static	buy872	;
double	static	buy873	;
double	static	buy874	;
double	static	buy875	;
double	static	buy876	;
double	static	buy877	;
double	static	buy878	;
double	static	buy879	;
double	static	buy880	;
double	static	buy881	;
double	static	buy882	;
double	static	buy883	;
double	static	buy884	;
double	static	buy885	;
double	static	buy886	;
double	static	buy887	;
double	static	buy888	;
double	static	buy889	;
double	static	buy890	;
double	static	buy891	;
double	static	buy892	;
double	static	buy893	;
double	static	buy894	;
double	static	buy895	;
double	static	buy896	;
double	static	buy897	;
double	static	buy898	;
double	static	buy899	;
double	static	buy900	;
double	static	buy901	;
double	static	buy902	;
double	static	buy903	;
double	static	buy904	;
double	static	buy905	;
double	static	buy906	;
double	static	buy907	;
double	static	buy908	;
double	static	buy909	;
double	static	buy910	;
double	static	buy911	;
double	static	buy912	;
double	static	buy913	;
double	static	buy914	;
double	static	buy915	;
double	static	buy916	;
double	static	buy917	;
double	static	buy918	;
double	static	buy919	;
double	static	buy920	;
double	static	buy921	;
double	static	buy922	;
double	static	buy923	;
double	static	buy924	;
double	static	buy925	;
double	static	buy926	;
double	static	buy927	;
double	static	buy928	;
double	static	buy929	;
double	static	buy930	;
double	static	buy931	;
double	static	buy932	;
double	static	buy933	;
double	static	buy934	;
double	static	buy935	;
double	static	buy936	;
double	static	buy937	;
double	static	buy938	;
double	static	buy939	;
double	static	buy940	;
double	static	buy941	;
double	static	buy942	;
double	static	buy943	;
double	static	buy944	;
double	static	buy945	;
double	static	buy946	;
double	static	buy947	;
double	static	buy948	;
double	static	buy949	;
double	static	buy950	;
double	static	buy951	;
double	static	buy952	;
double	static	buy953	;
double	static	buy954	;
double	static	buy955	;
double	static	buy956	;
double	static	buy957	;
double	static	buy958	;
double	static	buy959	;
double	static	buy960	;
double	static	buy961	;
double	static	buy962	;
double	static	buy963	;
double	static	buy964	;
double	static	buy965	;
double	static	buy966	;
double	static	buy967	;
double	static	buy968	;
double	static	buy969	;
double	static	buy970	;
double	static	buy971	;
double	static	buy972	;
double	static	buy973	;
double	static	buy974	;
double	static	buy975	;
double	static	buy976	;
double	static	buy977	;
double	static	buy978	;
double	static	buy979	;
double	static	buy980	;
double	static	buy981	;
double	static	buy982	;
double	static	buy983	;
double	static	buy984	;
double	static	buy985	;
double	static	buy986	;
double	static	buy987	;
double	static	buy988	;
double	static	buy989	;
double	static	buy990	;
double	static	buy991	;
double	static	buy992	;
double	static	buy993	;
double	static	buy994	;
double	static	buy995	;
double	static	buy996	;
double	static	buy997	;
double	static	buy998	;
double	static	buy999	;
double	static	buy1000	;


//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
  ObjectDelete("Average_Price_Line_"+Symbol());
   ObjectDelete("Information_"+Symbol());
//---
   NrOfDigits=Digits;
//---
   if(NrOfDigits==5 || NrOfDigits==3) PipAdjust=10;
   else
      if(NrOfDigits==4 || NrOfDigits==2) PipAdjust=1;
//---
   point=Point*PipAdjust;
//---
   double ticksize = MarketInfo(Symbol(),MODE_TICKSIZE);      
   if(ticksize ==0.0001||ticksize ==0.001)
   pips = ticksize * 10;
   else pips = ticksize;
   
  double	ok1	=	GlobalVariableGet	(	test1	)	;
double	ok2	=	GlobalVariableGet	(	test2	)	;
double	ok3	=	GlobalVariableGet	(	test3	)	;
double	ok4	=	GlobalVariableGet	(	test4	)	;
double	ok5	=	GlobalVariableGet	(	test5	)	;
double	ok6	=	GlobalVariableGet	(	test6	)	;
double	ok7	=	GlobalVariableGet	(	test7	)	;
double	ok8	=	GlobalVariableGet	(	test8	)	;
double	ok9	=	GlobalVariableGet	(	test9	)	;
double	ok10	=	GlobalVariableGet	(	test10	)	;
double	ok11	=	GlobalVariableGet	(	test11	)	;
double	ok12	=	GlobalVariableGet	(	test12	)	;
double	ok13	=	GlobalVariableGet	(	test13	)	;
double	ok14	=	GlobalVariableGet	(	test14	)	;
double	ok15	=	GlobalVariableGet	(	test15	)	;
double	ok16	=	GlobalVariableGet	(	test16	)	;
double	ok17	=	GlobalVariableGet	(	test17	)	;
double	ok18	=	GlobalVariableGet	(	test18	)	;
double	ok19	=	GlobalVariableGet	(	test19	)	;
double	ok20	=	GlobalVariableGet	(	test20	)	;
double	ok21	=	GlobalVariableGet	(	test21	)	;
double	ok22	=	GlobalVariableGet	(	test22	)	;
double	ok23	=	GlobalVariableGet	(	test23	)	;
double	ok24	=	GlobalVariableGet	(	test24	)	;
double	ok25	=	GlobalVariableGet	(	test25	)	;
double	ok26	=	GlobalVariableGet	(	test26	)	;
double	ok27	=	GlobalVariableGet	(	test27	)	;
double	ok28	=	GlobalVariableGet	(	test28	)	;
double	ok29	=	GlobalVariableGet	(	test29	)	;
double	ok30	=	GlobalVariableGet	(	test30	)	;
double	ok31	=	GlobalVariableGet	(	test31	)	;
double	ok32	=	GlobalVariableGet	(	test32	)	;
double	ok33	=	GlobalVariableGet	(	test33	)	;
double	ok34	=	GlobalVariableGet	(	test34	)	;
double	ok35	=	GlobalVariableGet	(	test35	)	;
double	ok36	=	GlobalVariableGet	(	test36	)	;
double	ok37	=	GlobalVariableGet	(	test37	)	;
double	ok38	=	GlobalVariableGet	(	test38	)	;
double	ok39	=	GlobalVariableGet	(	test39	)	;
double	ok40	=	GlobalVariableGet	(	test40	)	;
double	ok41	=	GlobalVariableGet	(	test41	)	;
double	ok42	=	GlobalVariableGet	(	test42	)	;
double	ok43	=	GlobalVariableGet	(	test43	)	;
double	ok44	=	GlobalVariableGet	(	test44	)	;
double	ok45	=	GlobalVariableGet	(	test45	)	;
double	ok46	=	GlobalVariableGet	(	test46	)	;
double	ok47	=	GlobalVariableGet	(	test47	)	;
double	ok48	=	GlobalVariableGet	(	test48	)	;
double	ok49	=	GlobalVariableGet	(	test49	)	;
double	ok50	=	GlobalVariableGet	(	test50	)	;
double	ok51	=	GlobalVariableGet	(	test51	)	;
double	ok52	=	GlobalVariableGet	(	test52	)	;
double	ok53	=	GlobalVariableGet	(	test53	)	;
double	ok54	=	GlobalVariableGet	(	test54	)	;
double	ok55	=	GlobalVariableGet	(	test55	)	;
double	ok56	=	GlobalVariableGet	(	test56	)	;
double	ok57	=	GlobalVariableGet	(	test57	)	;
double	ok58	=	GlobalVariableGet	(	test58	)	;
double	ok59	=	GlobalVariableGet	(	test59	)	;
double	ok60	=	GlobalVariableGet	(	test60	)	;
double	ok61	=	GlobalVariableGet	(	test61	)	;
double	ok62	=	GlobalVariableGet	(	test62	)	;
double	ok63	=	GlobalVariableGet	(	test63	)	;
double	ok64	=	GlobalVariableGet	(	test64	)	;
double	ok65	=	GlobalVariableGet	(	test65	)	;
double	ok66	=	GlobalVariableGet	(	test66	)	;
double	ok67	=	GlobalVariableGet	(	test67	)	;
double	ok68	=	GlobalVariableGet	(	test68	)	;
double	ok69	=	GlobalVariableGet	(	test69	)	;
double	ok70	=	GlobalVariableGet	(	test70	)	;
double	ok71	=	GlobalVariableGet	(	test71	)	;
double	ok72	=	GlobalVariableGet	(	test72	)	;
double	ok73	=	GlobalVariableGet	(	test73	)	;
double	ok74	=	GlobalVariableGet	(	test74	)	;
double	ok75	=	GlobalVariableGet	(	test75	)	;
double	ok76	=	GlobalVariableGet	(	test76	)	;
double	ok77	=	GlobalVariableGet	(	test77	)	;
double	ok78	=	GlobalVariableGet	(	test78	)	;
double	ok79	=	GlobalVariableGet	(	test79	)	;
double	ok80	=	GlobalVariableGet	(	test80	)	;
double	ok81	=	GlobalVariableGet	(	test81	)	;
double	ok82	=	GlobalVariableGet	(	test82	)	;
double	ok83	=	GlobalVariableGet	(	test83	)	;
double	ok84	=	GlobalVariableGet	(	test84	)	;
double	ok85	=	GlobalVariableGet	(	test85	)	;
double	ok86	=	GlobalVariableGet	(	test86	)	;
double	ok87	=	GlobalVariableGet	(	test87	)	;
double	ok88	=	GlobalVariableGet	(	test88	)	;
double	ok89	=	GlobalVariableGet	(	test89	)	;
double	ok90	=	GlobalVariableGet	(	test90	)	;
double	ok91	=	GlobalVariableGet	(	test91	)	;
double	ok92	=	GlobalVariableGet	(	test92	)	;
double	ok93	=	GlobalVariableGet	(	test93	)	;
double	ok94	=	GlobalVariableGet	(	test94	)	;
double	ok95	=	GlobalVariableGet	(	test95	)	;
double	ok96	=	GlobalVariableGet	(	test96	)	;
double	ok97	=	GlobalVariableGet	(	test97	)	;
double	ok98	=	GlobalVariableGet	(	test98	)	;
double	ok99	=	GlobalVariableGet	(	test99	)	;
double	ok100	=	GlobalVariableGet	(	test100	)	;
double	ok101	=	GlobalVariableGet	(	test101	)	;
double	ok102	=	GlobalVariableGet	(	test102	)	;
double	ok103	=	GlobalVariableGet	(	test103	)	;
double	ok104	=	GlobalVariableGet	(	test104	)	;
double	ok105	=	GlobalVariableGet	(	test105	)	;
double	ok106	=	GlobalVariableGet	(	test106	)	;
double	ok107	=	GlobalVariableGet	(	test107	)	;
double	ok108	=	GlobalVariableGet	(	test108	)	;
double	ok109	=	GlobalVariableGet	(	test109	)	;
double	ok110	=	GlobalVariableGet	(	test110	)	;
double	ok111	=	GlobalVariableGet	(	test111	)	;
double	ok112	=	GlobalVariableGet	(	test112	)	;
double	ok113	=	GlobalVariableGet	(	test113	)	;
double	ok114	=	GlobalVariableGet	(	test114	)	;
double	ok115	=	GlobalVariableGet	(	test115	)	;
double	ok116	=	GlobalVariableGet	(	test116	)	;
double	ok117	=	GlobalVariableGet	(	test117	)	;
double	ok118	=	GlobalVariableGet	(	test118	)	;
double	ok119	=	GlobalVariableGet	(	test119	)	;
double	ok120	=	GlobalVariableGet	(	test120	)	;
double	ok121	=	GlobalVariableGet	(	test121	)	;
double	ok122	=	GlobalVariableGet	(	test122	)	;
double	ok123	=	GlobalVariableGet	(	test123	)	;
double	ok124	=	GlobalVariableGet	(	test124	)	;
double	ok125	=	GlobalVariableGet	(	test125	)	;
double	ok126	=	GlobalVariableGet	(	test126	)	;
double	ok127	=	GlobalVariableGet	(	test127	)	;
double	ok128	=	GlobalVariableGet	(	test128	)	;
double	ok129	=	GlobalVariableGet	(	test129	)	;
double	ok130	=	GlobalVariableGet	(	test130	)	;
double	ok131	=	GlobalVariableGet	(	test131	)	;
double	ok132	=	GlobalVariableGet	(	test132	)	;
double	ok133	=	GlobalVariableGet	(	test133	)	;
double	ok134	=	GlobalVariableGet	(	test134	)	;
double	ok135	=	GlobalVariableGet	(	test135	)	;
double	ok136	=	GlobalVariableGet	(	test136	)	;
double	ok137	=	GlobalVariableGet	(	test137	)	;
double	ok138	=	GlobalVariableGet	(	test138	)	;
double	ok139	=	GlobalVariableGet	(	test139	)	;
double	ok140	=	GlobalVariableGet	(	test140	)	;
double	ok141	=	GlobalVariableGet	(	test141	)	;
double	ok142	=	GlobalVariableGet	(	test142	)	;
double	ok143	=	GlobalVariableGet	(	test143	)	;
double	ok144	=	GlobalVariableGet	(	test144	)	;
double	ok145	=	GlobalVariableGet	(	test145	)	;
double	ok146	=	GlobalVariableGet	(	test146	)	;
double	ok147	=	GlobalVariableGet	(	test147	)	;
double	ok148	=	GlobalVariableGet	(	test148	)	;
double	ok149	=	GlobalVariableGet	(	test149	)	;
double	ok150	=	GlobalVariableGet	(	test150	)	;
double	ok151	=	GlobalVariableGet	(	test151	)	;
double	ok152	=	GlobalVariableGet	(	test152	)	;
double	ok153	=	GlobalVariableGet	(	test153	)	;
double	ok154	=	GlobalVariableGet	(	test154	)	;
double	ok155	=	GlobalVariableGet	(	test155	)	;
double	ok156	=	GlobalVariableGet	(	test156	)	;
double	ok157	=	GlobalVariableGet	(	test157	)	;
double	ok158	=	GlobalVariableGet	(	test158	)	;
double	ok159	=	GlobalVariableGet	(	test159	)	;
double	ok160	=	GlobalVariableGet	(	test160	)	;
double	ok161	=	GlobalVariableGet	(	test161	)	;
double	ok162	=	GlobalVariableGet	(	test162	)	;
double	ok163	=	GlobalVariableGet	(	test163	)	;
double	ok164	=	GlobalVariableGet	(	test164	)	;
double	ok165	=	GlobalVariableGet	(	test165	)	;
double	ok166	=	GlobalVariableGet	(	test166	)	;
double	ok167	=	GlobalVariableGet	(	test167	)	;
double	ok168	=	GlobalVariableGet	(	test168	)	;
double	ok169	=	GlobalVariableGet	(	test169	)	;
double	ok170	=	GlobalVariableGet	(	test170	)	;
double	ok171	=	GlobalVariableGet	(	test171	)	;
double	ok172	=	GlobalVariableGet	(	test172	)	;
double	ok173	=	GlobalVariableGet	(	test173	)	;
double	ok174	=	GlobalVariableGet	(	test174	)	;
double	ok175	=	GlobalVariableGet	(	test175	)	;
double	ok176	=	GlobalVariableGet	(	test176	)	;
double	ok177	=	GlobalVariableGet	(	test177	)	;
double	ok178	=	GlobalVariableGet	(	test178	)	;
double	ok179	=	GlobalVariableGet	(	test179	)	;
double	ok180	=	GlobalVariableGet	(	test180	)	;
double	ok181	=	GlobalVariableGet	(	test181	)	;
double	ok182	=	GlobalVariableGet	(	test182	)	;
double	ok183	=	GlobalVariableGet	(	test183	)	;
double	ok184	=	GlobalVariableGet	(	test184	)	;
double	ok185	=	GlobalVariableGet	(	test185	)	;
double	ok186	=	GlobalVariableGet	(	test186	)	;
double	ok187	=	GlobalVariableGet	(	test187	)	;
double	ok188	=	GlobalVariableGet	(	test188	)	;
double	ok189	=	GlobalVariableGet	(	test189	)	;
double	ok190	=	GlobalVariableGet	(	test190	)	;
double	ok191	=	GlobalVariableGet	(	test191	)	;
double	ok192	=	GlobalVariableGet	(	test192	)	;
double	ok193	=	GlobalVariableGet	(	test193	)	;
double	ok194	=	GlobalVariableGet	(	test194	)	;
double	ok195	=	GlobalVariableGet	(	test195	)	;
double	ok196	=	GlobalVariableGet	(	test196	)	;
double	ok197	=	GlobalVariableGet	(	test197	)	;
double	ok198	=	GlobalVariableGet	(	test198	)	;
double	ok199	=	GlobalVariableGet	(	test199	)	;
double	ok200	=	GlobalVariableGet	(	test200	)	;
double	ok201	=	GlobalVariableGet	(	test201	)	;
double	ok202	=	GlobalVariableGet	(	test202	)	;
double	ok203	=	GlobalVariableGet	(	test203	)	;
double	ok204	=	GlobalVariableGet	(	test204	)	;
double	ok205	=	GlobalVariableGet	(	test205	)	;
double	ok206	=	GlobalVariableGet	(	test206	)	;
double	ok207	=	GlobalVariableGet	(	test207	)	;
double	ok208	=	GlobalVariableGet	(	test208	)	;
double	ok209	=	GlobalVariableGet	(	test209	)	;
double	ok210	=	GlobalVariableGet	(	test210	)	;
double	ok211	=	GlobalVariableGet	(	test211	)	;
double	ok212	=	GlobalVariableGet	(	test212	)	;
double	ok213	=	GlobalVariableGet	(	test213	)	;
double	ok214	=	GlobalVariableGet	(	test214	)	;
double	ok215	=	GlobalVariableGet	(	test215	)	;
double	ok216	=	GlobalVariableGet	(	test216	)	;
double	ok217	=	GlobalVariableGet	(	test217	)	;
double	ok218	=	GlobalVariableGet	(	test218	)	;
double	ok219	=	GlobalVariableGet	(	test219	)	;
double	ok220	=	GlobalVariableGet	(	test220	)	;
double	ok221	=	GlobalVariableGet	(	test221	)	;
double	ok222	=	GlobalVariableGet	(	test222	)	;
double	ok223	=	GlobalVariableGet	(	test223	)	;
double	ok224	=	GlobalVariableGet	(	test224	)	;
double	ok225	=	GlobalVariableGet	(	test225	)	;
double	ok226	=	GlobalVariableGet	(	test226	)	;
double	ok227	=	GlobalVariableGet	(	test227	)	;
double	ok228	=	GlobalVariableGet	(	test228	)	;
double	ok229	=	GlobalVariableGet	(	test229	)	;
double	ok230	=	GlobalVariableGet	(	test230	)	;
double	ok231	=	GlobalVariableGet	(	test231	)	;
double	ok232	=	GlobalVariableGet	(	test232	)	;
double	ok233	=	GlobalVariableGet	(	test233	)	;
double	ok234	=	GlobalVariableGet	(	test234	)	;
double	ok235	=	GlobalVariableGet	(	test235	)	;
double	ok236	=	GlobalVariableGet	(	test236	)	;
double	ok237	=	GlobalVariableGet	(	test237	)	;
double	ok238	=	GlobalVariableGet	(	test238	)	;
double	ok239	=	GlobalVariableGet	(	test239	)	;
double	ok240	=	GlobalVariableGet	(	test240	)	;
double	ok241	=	GlobalVariableGet	(	test241	)	;
double	ok242	=	GlobalVariableGet	(	test242	)	;
double	ok243	=	GlobalVariableGet	(	test243	)	;
double	ok244	=	GlobalVariableGet	(	test244	)	;
double	ok245	=	GlobalVariableGet	(	test245	)	;
double	ok246	=	GlobalVariableGet	(	test246	)	;
double	ok247	=	GlobalVariableGet	(	test247	)	;
double	ok248	=	GlobalVariableGet	(	test248	)	;
double	ok249	=	GlobalVariableGet	(	test249	)	;
double	ok250	=	GlobalVariableGet	(	test250	)	;
double	ok251	=	GlobalVariableGet	(	test251	)	;
double	ok252	=	GlobalVariableGet	(	test252	)	;
double	ok253	=	GlobalVariableGet	(	test253	)	;
double	ok254	=	GlobalVariableGet	(	test254	)	;
double	ok255	=	GlobalVariableGet	(	test255	)	;
double	ok256	=	GlobalVariableGet	(	test256	)	;
double	ok257	=	GlobalVariableGet	(	test257	)	;
double	ok258	=	GlobalVariableGet	(	test258	)	;
double	ok259	=	GlobalVariableGet	(	test259	)	;
double	ok260	=	GlobalVariableGet	(	test260	)	;
double	ok261	=	GlobalVariableGet	(	test261	)	;
double	ok262	=	GlobalVariableGet	(	test262	)	;
double	ok263	=	GlobalVariableGet	(	test263	)	;
double	ok264	=	GlobalVariableGet	(	test264	)	;
double	ok265	=	GlobalVariableGet	(	test265	)	;
double	ok266	=	GlobalVariableGet	(	test266	)	;
double	ok267	=	GlobalVariableGet	(	test267	)	;
double	ok268	=	GlobalVariableGet	(	test268	)	;
double	ok269	=	GlobalVariableGet	(	test269	)	;
double	ok270	=	GlobalVariableGet	(	test270	)	;
double	ok271	=	GlobalVariableGet	(	test271	)	;
double	ok272	=	GlobalVariableGet	(	test272	)	;
double	ok273	=	GlobalVariableGet	(	test273	)	;
double	ok274	=	GlobalVariableGet	(	test274	)	;
double	ok275	=	GlobalVariableGet	(	test275	)	;
double	ok276	=	GlobalVariableGet	(	test276	)	;
double	ok277	=	GlobalVariableGet	(	test277	)	;
double	ok278	=	GlobalVariableGet	(	test278	)	;
double	ok279	=	GlobalVariableGet	(	test279	)	;
double	ok280	=	GlobalVariableGet	(	test280	)	;
double	ok281	=	GlobalVariableGet	(	test281	)	;
double	ok282	=	GlobalVariableGet	(	test282	)	;
double	ok283	=	GlobalVariableGet	(	test283	)	;
double	ok284	=	GlobalVariableGet	(	test284	)	;
double	ok285	=	GlobalVariableGet	(	test285	)	;
double	ok286	=	GlobalVariableGet	(	test286	)	;
double	ok287	=	GlobalVariableGet	(	test287	)	;
double	ok288	=	GlobalVariableGet	(	test288	)	;
double	ok289	=	GlobalVariableGet	(	test289	)	;
double	ok290	=	GlobalVariableGet	(	test290	)	;
double	ok291	=	GlobalVariableGet	(	test291	)	;
double	ok292	=	GlobalVariableGet	(	test292	)	;
double	ok293	=	GlobalVariableGet	(	test293	)	;
double	ok294	=	GlobalVariableGet	(	test294	)	;
double	ok295	=	GlobalVariableGet	(	test295	)	;
double	ok296	=	GlobalVariableGet	(	test296	)	;
double	ok297	=	GlobalVariableGet	(	test297	)	;
double	ok298	=	GlobalVariableGet	(	test298	)	;
double	ok299	=	GlobalVariableGet	(	test299	)	;
double	ok300	=	GlobalVariableGet	(	test300	)	;
double	ok301	=	GlobalVariableGet	(	test301	)	;
double	ok302	=	GlobalVariableGet	(	test302	)	;
double	ok303	=	GlobalVariableGet	(	test303	)	;
double	ok304	=	GlobalVariableGet	(	test304	)	;
double	ok305	=	GlobalVariableGet	(	test305	)	;
double	ok306	=	GlobalVariableGet	(	test306	)	;
double	ok307	=	GlobalVariableGet	(	test307	)	;
double	ok308	=	GlobalVariableGet	(	test308	)	;
double	ok309	=	GlobalVariableGet	(	test309	)	;
double	ok310	=	GlobalVariableGet	(	test310	)	;
double	ok311	=	GlobalVariableGet	(	test311	)	;
double	ok312	=	GlobalVariableGet	(	test312	)	;
double	ok313	=	GlobalVariableGet	(	test313	)	;
double	ok314	=	GlobalVariableGet	(	test314	)	;
double	ok315	=	GlobalVariableGet	(	test315	)	;
double	ok316	=	GlobalVariableGet	(	test316	)	;
double	ok317	=	GlobalVariableGet	(	test317	)	;
double	ok318	=	GlobalVariableGet	(	test318	)	;
double	ok319	=	GlobalVariableGet	(	test319	)	;
double	ok320	=	GlobalVariableGet	(	test320	)	;
double	ok321	=	GlobalVariableGet	(	test321	)	;
double	ok322	=	GlobalVariableGet	(	test322	)	;
double	ok323	=	GlobalVariableGet	(	test323	)	;
double	ok324	=	GlobalVariableGet	(	test324	)	;
double	ok325	=	GlobalVariableGet	(	test325	)	;
double	ok326	=	GlobalVariableGet	(	test326	)	;
double	ok327	=	GlobalVariableGet	(	test327	)	;
double	ok328	=	GlobalVariableGet	(	test328	)	;
double	ok329	=	GlobalVariableGet	(	test329	)	;
double	ok330	=	GlobalVariableGet	(	test330	)	;
double	ok331	=	GlobalVariableGet	(	test331	)	;
double	ok332	=	GlobalVariableGet	(	test332	)	;
double	ok333	=	GlobalVariableGet	(	test333	)	;
double	ok334	=	GlobalVariableGet	(	test334	)	;
double	ok335	=	GlobalVariableGet	(	test335	)	;
double	ok336	=	GlobalVariableGet	(	test336	)	;
double	ok337	=	GlobalVariableGet	(	test337	)	;
double	ok338	=	GlobalVariableGet	(	test338	)	;
double	ok339	=	GlobalVariableGet	(	test339	)	;
double	ok340	=	GlobalVariableGet	(	test340	)	;
double	ok341	=	GlobalVariableGet	(	test341	)	;
double	ok342	=	GlobalVariableGet	(	test342	)	;
double	ok343	=	GlobalVariableGet	(	test343	)	;
double	ok344	=	GlobalVariableGet	(	test344	)	;
double	ok345	=	GlobalVariableGet	(	test345	)	;
double	ok346	=	GlobalVariableGet	(	test346	)	;
double	ok347	=	GlobalVariableGet	(	test347	)	;
double	ok348	=	GlobalVariableGet	(	test348	)	;
double	ok349	=	GlobalVariableGet	(	test349	)	;
double	ok350	=	GlobalVariableGet	(	test350	)	;
double	ok351	=	GlobalVariableGet	(	test351	)	;
double	ok352	=	GlobalVariableGet	(	test352	)	;
double	ok353	=	GlobalVariableGet	(	test353	)	;
double	ok354	=	GlobalVariableGet	(	test354	)	;
double	ok355	=	GlobalVariableGet	(	test355	)	;
double	ok356	=	GlobalVariableGet	(	test356	)	;
double	ok357	=	GlobalVariableGet	(	test357	)	;
double	ok358	=	GlobalVariableGet	(	test358	)	;
double	ok359	=	GlobalVariableGet	(	test359	)	;
double	ok360	=	GlobalVariableGet	(	test360	)	;
double	ok361	=	GlobalVariableGet	(	test361	)	;
double	ok362	=	GlobalVariableGet	(	test362	)	;
double	ok363	=	GlobalVariableGet	(	test363	)	;
double	ok364	=	GlobalVariableGet	(	test364	)	;
double	ok365	=	GlobalVariableGet	(	test365	)	;
double	ok366	=	GlobalVariableGet	(	test366	)	;
double	ok367	=	GlobalVariableGet	(	test367	)	;
double	ok368	=	GlobalVariableGet	(	test368	)	;
double	ok369	=	GlobalVariableGet	(	test369	)	;
double	ok370	=	GlobalVariableGet	(	test370	)	;
double	ok371	=	GlobalVariableGet	(	test371	)	;
double	ok372	=	GlobalVariableGet	(	test372	)	;
double	ok373	=	GlobalVariableGet	(	test373	)	;
double	ok374	=	GlobalVariableGet	(	test374	)	;
double	ok375	=	GlobalVariableGet	(	test375	)	;
double	ok376	=	GlobalVariableGet	(	test376	)	;
double	ok377	=	GlobalVariableGet	(	test377	)	;
double	ok378	=	GlobalVariableGet	(	test378	)	;
double	ok379	=	GlobalVariableGet	(	test379	)	;
double	ok380	=	GlobalVariableGet	(	test380	)	;
double	ok381	=	GlobalVariableGet	(	test381	)	;
double	ok382	=	GlobalVariableGet	(	test382	)	;
double	ok383	=	GlobalVariableGet	(	test383	)	;
double	ok384	=	GlobalVariableGet	(	test384	)	;
double	ok385	=	GlobalVariableGet	(	test385	)	;
double	ok386	=	GlobalVariableGet	(	test386	)	;
double	ok387	=	GlobalVariableGet	(	test387	)	;
double	ok388	=	GlobalVariableGet	(	test388	)	;
double	ok389	=	GlobalVariableGet	(	test389	)	;
double	ok390	=	GlobalVariableGet	(	test390	)	;
double	ok391	=	GlobalVariableGet	(	test391	)	;
double	ok392	=	GlobalVariableGet	(	test392	)	;
double	ok393	=	GlobalVariableGet	(	test393	)	;
double	ok394	=	GlobalVariableGet	(	test394	)	;
double	ok395	=	GlobalVariableGet	(	test395	)	;
double	ok396	=	GlobalVariableGet	(	test396	)	;
double	ok397	=	GlobalVariableGet	(	test397	)	;
double	ok398	=	GlobalVariableGet	(	test398	)	;
double	ok399	=	GlobalVariableGet	(	test399	)	;
double	ok400	=	GlobalVariableGet	(	test400	)	;
double	ok401	=	GlobalVariableGet	(	test401	)	;
double	ok402	=	GlobalVariableGet	(	test402	)	;
double	ok403	=	GlobalVariableGet	(	test403	)	;
double	ok404	=	GlobalVariableGet	(	test404	)	;
double	ok405	=	GlobalVariableGet	(	test405	)	;
double	ok406	=	GlobalVariableGet	(	test406	)	;
double	ok407	=	GlobalVariableGet	(	test407	)	;
double	ok408	=	GlobalVariableGet	(	test408	)	;
double	ok409	=	GlobalVariableGet	(	test409	)	;
double	ok410	=	GlobalVariableGet	(	test410	)	;
double	ok411	=	GlobalVariableGet	(	test411	)	;
double	ok412	=	GlobalVariableGet	(	test412	)	;
double	ok413	=	GlobalVariableGet	(	test413	)	;
double	ok414	=	GlobalVariableGet	(	test414	)	;
double	ok415	=	GlobalVariableGet	(	test415	)	;
double	ok416	=	GlobalVariableGet	(	test416	)	;
double	ok417	=	GlobalVariableGet	(	test417	)	;
double	ok418	=	GlobalVariableGet	(	test418	)	;
double	ok419	=	GlobalVariableGet	(	test419	)	;
double	ok420	=	GlobalVariableGet	(	test420	)	;
double	ok421	=	GlobalVariableGet	(	test421	)	;
double	ok422	=	GlobalVariableGet	(	test422	)	;
double	ok423	=	GlobalVariableGet	(	test423	)	;
double	ok424	=	GlobalVariableGet	(	test424	)	;
double	ok425	=	GlobalVariableGet	(	test425	)	;
double	ok426	=	GlobalVariableGet	(	test426	)	;
double	ok427	=	GlobalVariableGet	(	test427	)	;
double	ok428	=	GlobalVariableGet	(	test428	)	;
double	ok429	=	GlobalVariableGet	(	test429	)	;
double	ok430	=	GlobalVariableGet	(	test430	)	;
double	ok431	=	GlobalVariableGet	(	test431	)	;
double	ok432	=	GlobalVariableGet	(	test432	)	;
double	ok433	=	GlobalVariableGet	(	test433	)	;
double	ok434	=	GlobalVariableGet	(	test434	)	;
double	ok435	=	GlobalVariableGet	(	test435	)	;
double	ok436	=	GlobalVariableGet	(	test436	)	;
double	ok437	=	GlobalVariableGet	(	test437	)	;
double	ok438	=	GlobalVariableGet	(	test438	)	;
double	ok439	=	GlobalVariableGet	(	test439	)	;
double	ok440	=	GlobalVariableGet	(	test440	)	;
double	ok441	=	GlobalVariableGet	(	test441	)	;
double	ok442	=	GlobalVariableGet	(	test442	)	;
double	ok443	=	GlobalVariableGet	(	test443	)	;
double	ok444	=	GlobalVariableGet	(	test444	)	;
double	ok445	=	GlobalVariableGet	(	test445	)	;
double	ok446	=	GlobalVariableGet	(	test446	)	;
double	ok447	=	GlobalVariableGet	(	test447	)	;
double	ok448	=	GlobalVariableGet	(	test448	)	;
double	ok449	=	GlobalVariableGet	(	test449	)	;
double	ok450	=	GlobalVariableGet	(	test450	)	;
double	ok451	=	GlobalVariableGet	(	test451	)	;
double	ok452	=	GlobalVariableGet	(	test452	)	;
double	ok453	=	GlobalVariableGet	(	test453	)	;
double	ok454	=	GlobalVariableGet	(	test454	)	;
double	ok455	=	GlobalVariableGet	(	test455	)	;
double	ok456	=	GlobalVariableGet	(	test456	)	;
double	ok457	=	GlobalVariableGet	(	test457	)	;
double	ok458	=	GlobalVariableGet	(	test458	)	;
double	ok459	=	GlobalVariableGet	(	test459	)	;
double	ok460	=	GlobalVariableGet	(	test460	)	;
double	ok461	=	GlobalVariableGet	(	test461	)	;
double	ok462	=	GlobalVariableGet	(	test462	)	;
double	ok463	=	GlobalVariableGet	(	test463	)	;
double	ok464	=	GlobalVariableGet	(	test464	)	;
double	ok465	=	GlobalVariableGet	(	test465	)	;
double	ok466	=	GlobalVariableGet	(	test466	)	;
double	ok467	=	GlobalVariableGet	(	test467	)	;
double	ok468	=	GlobalVariableGet	(	test468	)	;
double	ok469	=	GlobalVariableGet	(	test469	)	;
double	ok470	=	GlobalVariableGet	(	test470	)	;
double	ok471	=	GlobalVariableGet	(	test471	)	;
double	ok472	=	GlobalVariableGet	(	test472	)	;
double	ok473	=	GlobalVariableGet	(	test473	)	;
double	ok474	=	GlobalVariableGet	(	test474	)	;
double	ok475	=	GlobalVariableGet	(	test475	)	;
double	ok476	=	GlobalVariableGet	(	test476	)	;
double	ok477	=	GlobalVariableGet	(	test477	)	;
double	ok478	=	GlobalVariableGet	(	test478	)	;
double	ok479	=	GlobalVariableGet	(	test479	)	;
double	ok480	=	GlobalVariableGet	(	test480	)	;
double	ok481	=	GlobalVariableGet	(	test481	)	;
double	ok482	=	GlobalVariableGet	(	test482	)	;
double	ok483	=	GlobalVariableGet	(	test483	)	;
double	ok484	=	GlobalVariableGet	(	test484	)	;
double	ok485	=	GlobalVariableGet	(	test485	)	;
double	ok486	=	GlobalVariableGet	(	test486	)	;
double	ok487	=	GlobalVariableGet	(	test487	)	;
double	ok488	=	GlobalVariableGet	(	test488	)	;
double	ok489	=	GlobalVariableGet	(	test489	)	;
double	ok490	=	GlobalVariableGet	(	test490	)	;
double	ok491	=	GlobalVariableGet	(	test491	)	;
double	ok492	=	GlobalVariableGet	(	test492	)	;
double	ok493	=	GlobalVariableGet	(	test493	)	;
double	ok494	=	GlobalVariableGet	(	test494	)	;
double	ok495	=	GlobalVariableGet	(	test495	)	;
double	ok496	=	GlobalVariableGet	(	test496	)	;
double	ok497	=	GlobalVariableGet	(	test497	)	;
double	ok498	=	GlobalVariableGet	(	test498	)	;
double	ok499	=	GlobalVariableGet	(	test499	)	;
double	ok500	=	GlobalVariableGet	(	test500	)	;
double	ok501	=	GlobalVariableGet	(	test501	)	;
double	ok502	=	GlobalVariableGet	(	test502	)	;
double	ok503	=	GlobalVariableGet	(	test503	)	;
double	ok504	=	GlobalVariableGet	(	test504	)	;
double	ok505	=	GlobalVariableGet	(	test505	)	;
double	ok506	=	GlobalVariableGet	(	test506	)	;
double	ok507	=	GlobalVariableGet	(	test507	)	;
double	ok508	=	GlobalVariableGet	(	test508	)	;
double	ok509	=	GlobalVariableGet	(	test509	)	;
double	ok510	=	GlobalVariableGet	(	test510	)	;
double	ok511	=	GlobalVariableGet	(	test511	)	;
double	ok512	=	GlobalVariableGet	(	test512	)	;
double	ok513	=	GlobalVariableGet	(	test513	)	;
double	ok514	=	GlobalVariableGet	(	test514	)	;
double	ok515	=	GlobalVariableGet	(	test515	)	;
double	ok516	=	GlobalVariableGet	(	test516	)	;
double	ok517	=	GlobalVariableGet	(	test517	)	;
double	ok518	=	GlobalVariableGet	(	test518	)	;
double	ok519	=	GlobalVariableGet	(	test519	)	;
double	ok520	=	GlobalVariableGet	(	test520	)	;
double	ok521	=	GlobalVariableGet	(	test521	)	;
double	ok522	=	GlobalVariableGet	(	test522	)	;
double	ok523	=	GlobalVariableGet	(	test523	)	;
double	ok524	=	GlobalVariableGet	(	test524	)	;
double	ok525	=	GlobalVariableGet	(	test525	)	;
double	ok526	=	GlobalVariableGet	(	test526	)	;
double	ok527	=	GlobalVariableGet	(	test527	)	;
double	ok528	=	GlobalVariableGet	(	test528	)	;
double	ok529	=	GlobalVariableGet	(	test529	)	;
double	ok530	=	GlobalVariableGet	(	test530	)	;
double	ok531	=	GlobalVariableGet	(	test531	)	;
double	ok532	=	GlobalVariableGet	(	test532	)	;
double	ok533	=	GlobalVariableGet	(	test533	)	;
double	ok534	=	GlobalVariableGet	(	test534	)	;
double	ok535	=	GlobalVariableGet	(	test535	)	;
double	ok536	=	GlobalVariableGet	(	test536	)	;
double	ok537	=	GlobalVariableGet	(	test537	)	;
double	ok538	=	GlobalVariableGet	(	test538	)	;
double	ok539	=	GlobalVariableGet	(	test539	)	;
double	ok540	=	GlobalVariableGet	(	test540	)	;
double	ok541	=	GlobalVariableGet	(	test541	)	;
double	ok542	=	GlobalVariableGet	(	test542	)	;
double	ok543	=	GlobalVariableGet	(	test543	)	;
double	ok544	=	GlobalVariableGet	(	test544	)	;
double	ok545	=	GlobalVariableGet	(	test545	)	;
double	ok546	=	GlobalVariableGet	(	test546	)	;
double	ok547	=	GlobalVariableGet	(	test547	)	;
double	ok548	=	GlobalVariableGet	(	test548	)	;
double	ok549	=	GlobalVariableGet	(	test549	)	;
double	ok550	=	GlobalVariableGet	(	test550	)	;
double	ok551	=	GlobalVariableGet	(	test551	)	;
double	ok552	=	GlobalVariableGet	(	test552	)	;
double	ok553	=	GlobalVariableGet	(	test553	)	;
double	ok554	=	GlobalVariableGet	(	test554	)	;
double	ok555	=	GlobalVariableGet	(	test555	)	;
double	ok556	=	GlobalVariableGet	(	test556	)	;
double	ok557	=	GlobalVariableGet	(	test557	)	;
double	ok558	=	GlobalVariableGet	(	test558	)	;
double	ok559	=	GlobalVariableGet	(	test559	)	;
double	ok560	=	GlobalVariableGet	(	test560	)	;
double	ok561	=	GlobalVariableGet	(	test561	)	;
double	ok562	=	GlobalVariableGet	(	test562	)	;
double	ok563	=	GlobalVariableGet	(	test563	)	;
double	ok564	=	GlobalVariableGet	(	test564	)	;
double	ok565	=	GlobalVariableGet	(	test565	)	;
double	ok566	=	GlobalVariableGet	(	test566	)	;
double	ok567	=	GlobalVariableGet	(	test567	)	;
double	ok568	=	GlobalVariableGet	(	test568	)	;
double	ok569	=	GlobalVariableGet	(	test569	)	;
double	ok570	=	GlobalVariableGet	(	test570	)	;
double	ok571	=	GlobalVariableGet	(	test571	)	;
double	ok572	=	GlobalVariableGet	(	test572	)	;
double	ok573	=	GlobalVariableGet	(	test573	)	;
double	ok574	=	GlobalVariableGet	(	test574	)	;
double	ok575	=	GlobalVariableGet	(	test575	)	;
double	ok576	=	GlobalVariableGet	(	test576	)	;
double	ok577	=	GlobalVariableGet	(	test577	)	;
double	ok578	=	GlobalVariableGet	(	test578	)	;
double	ok579	=	GlobalVariableGet	(	test579	)	;
double	ok580	=	GlobalVariableGet	(	test580	)	;
double	ok581	=	GlobalVariableGet	(	test581	)	;
double	ok582	=	GlobalVariableGet	(	test582	)	;
double	ok583	=	GlobalVariableGet	(	test583	)	;
double	ok584	=	GlobalVariableGet	(	test584	)	;
double	ok585	=	GlobalVariableGet	(	test585	)	;
double	ok586	=	GlobalVariableGet	(	test586	)	;
double	ok587	=	GlobalVariableGet	(	test587	)	;
double	ok588	=	GlobalVariableGet	(	test588	)	;
double	ok589	=	GlobalVariableGet	(	test589	)	;
double	ok590	=	GlobalVariableGet	(	test590	)	;
double	ok591	=	GlobalVariableGet	(	test591	)	;
double	ok592	=	GlobalVariableGet	(	test592	)	;
double	ok593	=	GlobalVariableGet	(	test593	)	;
double	ok594	=	GlobalVariableGet	(	test594	)	;
double	ok595	=	GlobalVariableGet	(	test595	)	;
double	ok596	=	GlobalVariableGet	(	test596	)	;
double	ok597	=	GlobalVariableGet	(	test597	)	;
double	ok598	=	GlobalVariableGet	(	test598	)	;
double	ok599	=	GlobalVariableGet	(	test599	)	;
double	ok600	=	GlobalVariableGet	(	test600	)	;
double	ok601	=	GlobalVariableGet	(	test601	)	;
double	ok602	=	GlobalVariableGet	(	test602	)	;
double	ok603	=	GlobalVariableGet	(	test603	)	;
double	ok604	=	GlobalVariableGet	(	test604	)	;
double	ok605	=	GlobalVariableGet	(	test605	)	;
double	ok606	=	GlobalVariableGet	(	test606	)	;
double	ok607	=	GlobalVariableGet	(	test607	)	;
double	ok608	=	GlobalVariableGet	(	test608	)	;
double	ok609	=	GlobalVariableGet	(	test609	)	;
double	ok610	=	GlobalVariableGet	(	test610	)	;
double	ok611	=	GlobalVariableGet	(	test611	)	;
double	ok612	=	GlobalVariableGet	(	test612	)	;
double	ok613	=	GlobalVariableGet	(	test613	)	;
double	ok614	=	GlobalVariableGet	(	test614	)	;
double	ok615	=	GlobalVariableGet	(	test615	)	;
double	ok616	=	GlobalVariableGet	(	test616	)	;
double	ok617	=	GlobalVariableGet	(	test617	)	;
double	ok618	=	GlobalVariableGet	(	test618	)	;
double	ok619	=	GlobalVariableGet	(	test619	)	;
double	ok620	=	GlobalVariableGet	(	test620	)	;
double	ok621	=	GlobalVariableGet	(	test621	)	;
double	ok622	=	GlobalVariableGet	(	test622	)	;
double	ok623	=	GlobalVariableGet	(	test623	)	;
double	ok624	=	GlobalVariableGet	(	test624	)	;
double	ok625	=	GlobalVariableGet	(	test625	)	;
double	ok626	=	GlobalVariableGet	(	test626	)	;
double	ok627	=	GlobalVariableGet	(	test627	)	;
double	ok628	=	GlobalVariableGet	(	test628	)	;
double	ok629	=	GlobalVariableGet	(	test629	)	;
double	ok630	=	GlobalVariableGet	(	test630	)	;
double	ok631	=	GlobalVariableGet	(	test631	)	;
double	ok632	=	GlobalVariableGet	(	test632	)	;
double	ok633	=	GlobalVariableGet	(	test633	)	;
double	ok634	=	GlobalVariableGet	(	test634	)	;
double	ok635	=	GlobalVariableGet	(	test635	)	;
double	ok636	=	GlobalVariableGet	(	test636	)	;
double	ok637	=	GlobalVariableGet	(	test637	)	;
double	ok638	=	GlobalVariableGet	(	test638	)	;
double	ok639	=	GlobalVariableGet	(	test639	)	;
double	ok640	=	GlobalVariableGet	(	test640	)	;
double	ok641	=	GlobalVariableGet	(	test641	)	;
double	ok642	=	GlobalVariableGet	(	test642	)	;
double	ok643	=	GlobalVariableGet	(	test643	)	;
double	ok644	=	GlobalVariableGet	(	test644	)	;
double	ok645	=	GlobalVariableGet	(	test645	)	;
double	ok646	=	GlobalVariableGet	(	test646	)	;
double	ok647	=	GlobalVariableGet	(	test647	)	;
double	ok648	=	GlobalVariableGet	(	test648	)	;
double	ok649	=	GlobalVariableGet	(	test649	)	;
double	ok650	=	GlobalVariableGet	(	test650	)	;
double	ok651	=	GlobalVariableGet	(	test651	)	;
double	ok652	=	GlobalVariableGet	(	test652	)	;
double	ok653	=	GlobalVariableGet	(	test653	)	;
double	ok654	=	GlobalVariableGet	(	test654	)	;
double	ok655	=	GlobalVariableGet	(	test655	)	;
double	ok656	=	GlobalVariableGet	(	test656	)	;
double	ok657	=	GlobalVariableGet	(	test657	)	;
double	ok658	=	GlobalVariableGet	(	test658	)	;
double	ok659	=	GlobalVariableGet	(	test659	)	;
double	ok660	=	GlobalVariableGet	(	test660	)	;
double	ok661	=	GlobalVariableGet	(	test661	)	;
double	ok662	=	GlobalVariableGet	(	test662	)	;
double	ok663	=	GlobalVariableGet	(	test663	)	;
double	ok664	=	GlobalVariableGet	(	test664	)	;
double	ok665	=	GlobalVariableGet	(	test665	)	;
double	ok666	=	GlobalVariableGet	(	test666	)	;
double	ok667	=	GlobalVariableGet	(	test667	)	;
double	ok668	=	GlobalVariableGet	(	test668	)	;
double	ok669	=	GlobalVariableGet	(	test669	)	;
double	ok670	=	GlobalVariableGet	(	test670	)	;
double	ok671	=	GlobalVariableGet	(	test671	)	;
double	ok672	=	GlobalVariableGet	(	test672	)	;
double	ok673	=	GlobalVariableGet	(	test673	)	;
double	ok674	=	GlobalVariableGet	(	test674	)	;
double	ok675	=	GlobalVariableGet	(	test675	)	;
double	ok676	=	GlobalVariableGet	(	test676	)	;
double	ok677	=	GlobalVariableGet	(	test677	)	;
double	ok678	=	GlobalVariableGet	(	test678	)	;
double	ok679	=	GlobalVariableGet	(	test679	)	;
double	ok680	=	GlobalVariableGet	(	test680	)	;
double	ok681	=	GlobalVariableGet	(	test681	)	;
double	ok682	=	GlobalVariableGet	(	test682	)	;
double	ok683	=	GlobalVariableGet	(	test683	)	;
double	ok684	=	GlobalVariableGet	(	test684	)	;
double	ok685	=	GlobalVariableGet	(	test685	)	;
double	ok686	=	GlobalVariableGet	(	test686	)	;
double	ok687	=	GlobalVariableGet	(	test687	)	;
double	ok688	=	GlobalVariableGet	(	test688	)	;
double	ok689	=	GlobalVariableGet	(	test689	)	;
double	ok690	=	GlobalVariableGet	(	test690	)	;
double	ok691	=	GlobalVariableGet	(	test691	)	;
double	ok692	=	GlobalVariableGet	(	test692	)	;
double	ok693	=	GlobalVariableGet	(	test693	)	;
double	ok694	=	GlobalVariableGet	(	test694	)	;
double	ok695	=	GlobalVariableGet	(	test695	)	;
double	ok696	=	GlobalVariableGet	(	test696	)	;
double	ok697	=	GlobalVariableGet	(	test697	)	;
double	ok698	=	GlobalVariableGet	(	test698	)	;
double	ok699	=	GlobalVariableGet	(	test699	)	;
double	ok700	=	GlobalVariableGet	(	test700	)	;
double	ok701	=	GlobalVariableGet	(	test701	)	;
double	ok702	=	GlobalVariableGet	(	test702	)	;
double	ok703	=	GlobalVariableGet	(	test703	)	;
double	ok704	=	GlobalVariableGet	(	test704	)	;
double	ok705	=	GlobalVariableGet	(	test705	)	;
double	ok706	=	GlobalVariableGet	(	test706	)	;
double	ok707	=	GlobalVariableGet	(	test707	)	;
double	ok708	=	GlobalVariableGet	(	test708	)	;
double	ok709	=	GlobalVariableGet	(	test709	)	;
double	ok710	=	GlobalVariableGet	(	test710	)	;
double	ok711	=	GlobalVariableGet	(	test711	)	;
double	ok712	=	GlobalVariableGet	(	test712	)	;
double	ok713	=	GlobalVariableGet	(	test713	)	;
double	ok714	=	GlobalVariableGet	(	test714	)	;
double	ok715	=	GlobalVariableGet	(	test715	)	;
double	ok716	=	GlobalVariableGet	(	test716	)	;
double	ok717	=	GlobalVariableGet	(	test717	)	;
double	ok718	=	GlobalVariableGet	(	test718	)	;
double	ok719	=	GlobalVariableGet	(	test719	)	;
double	ok720	=	GlobalVariableGet	(	test720	)	;
double	ok721	=	GlobalVariableGet	(	test721	)	;
double	ok722	=	GlobalVariableGet	(	test722	)	;
double	ok723	=	GlobalVariableGet	(	test723	)	;
double	ok724	=	GlobalVariableGet	(	test724	)	;
double	ok725	=	GlobalVariableGet	(	test725	)	;
double	ok726	=	GlobalVariableGet	(	test726	)	;
double	ok727	=	GlobalVariableGet	(	test727	)	;
double	ok728	=	GlobalVariableGet	(	test728	)	;
double	ok729	=	GlobalVariableGet	(	test729	)	;
double	ok730	=	GlobalVariableGet	(	test730	)	;
double	ok731	=	GlobalVariableGet	(	test731	)	;
double	ok732	=	GlobalVariableGet	(	test732	)	;
double	ok733	=	GlobalVariableGet	(	test733	)	;
double	ok734	=	GlobalVariableGet	(	test734	)	;
double	ok735	=	GlobalVariableGet	(	test735	)	;
double	ok736	=	GlobalVariableGet	(	test736	)	;
double	ok737	=	GlobalVariableGet	(	test737	)	;
double	ok738	=	GlobalVariableGet	(	test738	)	;
double	ok739	=	GlobalVariableGet	(	test739	)	;
double	ok740	=	GlobalVariableGet	(	test740	)	;
double	ok741	=	GlobalVariableGet	(	test741	)	;
double	ok742	=	GlobalVariableGet	(	test742	)	;
double	ok743	=	GlobalVariableGet	(	test743	)	;
double	ok744	=	GlobalVariableGet	(	test744	)	;
double	ok745	=	GlobalVariableGet	(	test745	)	;
double	ok746	=	GlobalVariableGet	(	test746	)	;
double	ok747	=	GlobalVariableGet	(	test747	)	;
double	ok748	=	GlobalVariableGet	(	test748	)	;
double	ok749	=	GlobalVariableGet	(	test749	)	;
double	ok750	=	GlobalVariableGet	(	test750	)	;
double	ok751	=	GlobalVariableGet	(	test751	)	;
double	ok752	=	GlobalVariableGet	(	test752	)	;
double	ok753	=	GlobalVariableGet	(	test753	)	;
double	ok754	=	GlobalVariableGet	(	test754	)	;
double	ok755	=	GlobalVariableGet	(	test755	)	;
double	ok756	=	GlobalVariableGet	(	test756	)	;
double	ok757	=	GlobalVariableGet	(	test757	)	;
double	ok758	=	GlobalVariableGet	(	test758	)	;
double	ok759	=	GlobalVariableGet	(	test759	)	;
double	ok760	=	GlobalVariableGet	(	test760	)	;
double	ok761	=	GlobalVariableGet	(	test761	)	;
double	ok762	=	GlobalVariableGet	(	test762	)	;
double	ok763	=	GlobalVariableGet	(	test763	)	;
double	ok764	=	GlobalVariableGet	(	test764	)	;
double	ok765	=	GlobalVariableGet	(	test765	)	;
double	ok766	=	GlobalVariableGet	(	test766	)	;
double	ok767	=	GlobalVariableGet	(	test767	)	;
double	ok768	=	GlobalVariableGet	(	test768	)	;
double	ok769	=	GlobalVariableGet	(	test769	)	;
double	ok770	=	GlobalVariableGet	(	test770	)	;
double	ok771	=	GlobalVariableGet	(	test771	)	;
double	ok772	=	GlobalVariableGet	(	test772	)	;
double	ok773	=	GlobalVariableGet	(	test773	)	;
double	ok774	=	GlobalVariableGet	(	test774	)	;
double	ok775	=	GlobalVariableGet	(	test775	)	;
double	ok776	=	GlobalVariableGet	(	test776	)	;
double	ok777	=	GlobalVariableGet	(	test777	)	;
double	ok778	=	GlobalVariableGet	(	test778	)	;
double	ok779	=	GlobalVariableGet	(	test779	)	;
double	ok780	=	GlobalVariableGet	(	test780	)	;
double	ok781	=	GlobalVariableGet	(	test781	)	;
double	ok782	=	GlobalVariableGet	(	test782	)	;
double	ok783	=	GlobalVariableGet	(	test783	)	;
double	ok784	=	GlobalVariableGet	(	test784	)	;
double	ok785	=	GlobalVariableGet	(	test785	)	;
double	ok786	=	GlobalVariableGet	(	test786	)	;
double	ok787	=	GlobalVariableGet	(	test787	)	;
double	ok788	=	GlobalVariableGet	(	test788	)	;
double	ok789	=	GlobalVariableGet	(	test789	)	;
double	ok790	=	GlobalVariableGet	(	test790	)	;
double	ok791	=	GlobalVariableGet	(	test791	)	;
double	ok792	=	GlobalVariableGet	(	test792	)	;
double	ok793	=	GlobalVariableGet	(	test793	)	;
double	ok794	=	GlobalVariableGet	(	test794	)	;
double	ok795	=	GlobalVariableGet	(	test795	)	;
double	ok796	=	GlobalVariableGet	(	test796	)	;
double	ok797	=	GlobalVariableGet	(	test797	)	;
double	ok798	=	GlobalVariableGet	(	test798	)	;
double	ok799	=	GlobalVariableGet	(	test799	)	;
double	ok800	=	GlobalVariableGet	(	test800	)	;
double	ok801	=	GlobalVariableGet	(	test801	)	;
double	ok802	=	GlobalVariableGet	(	test802	)	;
double	ok803	=	GlobalVariableGet	(	test803	)	;
double	ok804	=	GlobalVariableGet	(	test804	)	;
double	ok805	=	GlobalVariableGet	(	test805	)	;
double	ok806	=	GlobalVariableGet	(	test806	)	;
double	ok807	=	GlobalVariableGet	(	test807	)	;
double	ok808	=	GlobalVariableGet	(	test808	)	;
double	ok809	=	GlobalVariableGet	(	test809	)	;
double	ok810	=	GlobalVariableGet	(	test810	)	;
double	ok811	=	GlobalVariableGet	(	test811	)	;
double	ok812	=	GlobalVariableGet	(	test812	)	;
double	ok813	=	GlobalVariableGet	(	test813	)	;
double	ok814	=	GlobalVariableGet	(	test814	)	;
double	ok815	=	GlobalVariableGet	(	test815	)	;
double	ok816	=	GlobalVariableGet	(	test816	)	;
double	ok817	=	GlobalVariableGet	(	test817	)	;
double	ok818	=	GlobalVariableGet	(	test818	)	;
double	ok819	=	GlobalVariableGet	(	test819	)	;
double	ok820	=	GlobalVariableGet	(	test820	)	;
double	ok821	=	GlobalVariableGet	(	test821	)	;
double	ok822	=	GlobalVariableGet	(	test822	)	;
double	ok823	=	GlobalVariableGet	(	test823	)	;
double	ok824	=	GlobalVariableGet	(	test824	)	;
double	ok825	=	GlobalVariableGet	(	test825	)	;
double	ok826	=	GlobalVariableGet	(	test826	)	;
double	ok827	=	GlobalVariableGet	(	test827	)	;
double	ok828	=	GlobalVariableGet	(	test828	)	;
double	ok829	=	GlobalVariableGet	(	test829	)	;
double	ok830	=	GlobalVariableGet	(	test830	)	;
double	ok831	=	GlobalVariableGet	(	test831	)	;
double	ok832	=	GlobalVariableGet	(	test832	)	;
double	ok833	=	GlobalVariableGet	(	test833	)	;
double	ok834	=	GlobalVariableGet	(	test834	)	;
double	ok835	=	GlobalVariableGet	(	test835	)	;
double	ok836	=	GlobalVariableGet	(	test836	)	;
double	ok837	=	GlobalVariableGet	(	test837	)	;
double	ok838	=	GlobalVariableGet	(	test838	)	;
double	ok839	=	GlobalVariableGet	(	test839	)	;
double	ok840	=	GlobalVariableGet	(	test840	)	;
double	ok841	=	GlobalVariableGet	(	test841	)	;
double	ok842	=	GlobalVariableGet	(	test842	)	;
double	ok843	=	GlobalVariableGet	(	test843	)	;
double	ok844	=	GlobalVariableGet	(	test844	)	;
double	ok845	=	GlobalVariableGet	(	test845	)	;
double	ok846	=	GlobalVariableGet	(	test846	)	;
double	ok847	=	GlobalVariableGet	(	test847	)	;
double	ok848	=	GlobalVariableGet	(	test848	)	;
double	ok849	=	GlobalVariableGet	(	test849	)	;
double	ok850	=	GlobalVariableGet	(	test850	)	;
double	ok851	=	GlobalVariableGet	(	test851	)	;
double	ok852	=	GlobalVariableGet	(	test852	)	;
double	ok853	=	GlobalVariableGet	(	test853	)	;
double	ok854	=	GlobalVariableGet	(	test854	)	;
double	ok855	=	GlobalVariableGet	(	test855	)	;
double	ok856	=	GlobalVariableGet	(	test856	)	;
double	ok857	=	GlobalVariableGet	(	test857	)	;
double	ok858	=	GlobalVariableGet	(	test858	)	;
double	ok859	=	GlobalVariableGet	(	test859	)	;
double	ok860	=	GlobalVariableGet	(	test860	)	;
double	ok861	=	GlobalVariableGet	(	test861	)	;
double	ok862	=	GlobalVariableGet	(	test862	)	;
double	ok863	=	GlobalVariableGet	(	test863	)	;
double	ok864	=	GlobalVariableGet	(	test864	)	;
double	ok865	=	GlobalVariableGet	(	test865	)	;
double	ok866	=	GlobalVariableGet	(	test866	)	;
double	ok867	=	GlobalVariableGet	(	test867	)	;
double	ok868	=	GlobalVariableGet	(	test868	)	;
double	ok869	=	GlobalVariableGet	(	test869	)	;
double	ok870	=	GlobalVariableGet	(	test870	)	;
double	ok871	=	GlobalVariableGet	(	test871	)	;
double	ok872	=	GlobalVariableGet	(	test872	)	;
double	ok873	=	GlobalVariableGet	(	test873	)	;
double	ok874	=	GlobalVariableGet	(	test874	)	;
double	ok875	=	GlobalVariableGet	(	test875	)	;
double	ok876	=	GlobalVariableGet	(	test876	)	;
double	ok877	=	GlobalVariableGet	(	test877	)	;
double	ok878	=	GlobalVariableGet	(	test878	)	;
double	ok879	=	GlobalVariableGet	(	test879	)	;
double	ok880	=	GlobalVariableGet	(	test880	)	;
double	ok881	=	GlobalVariableGet	(	test881	)	;
double	ok882	=	GlobalVariableGet	(	test882	)	;
double	ok883	=	GlobalVariableGet	(	test883	)	;
double	ok884	=	GlobalVariableGet	(	test884	)	;
double	ok885	=	GlobalVariableGet	(	test885	)	;
double	ok886	=	GlobalVariableGet	(	test886	)	;
double	ok887	=	GlobalVariableGet	(	test887	)	;
double	ok888	=	GlobalVariableGet	(	test888	)	;
double	ok889	=	GlobalVariableGet	(	test889	)	;
double	ok890	=	GlobalVariableGet	(	test890	)	;
double	ok891	=	GlobalVariableGet	(	test891	)	;
double	ok892	=	GlobalVariableGet	(	test892	)	;
double	ok893	=	GlobalVariableGet	(	test893	)	;
double	ok894	=	GlobalVariableGet	(	test894	)	;
double	ok895	=	GlobalVariableGet	(	test895	)	;
double	ok896	=	GlobalVariableGet	(	test896	)	;
double	ok897	=	GlobalVariableGet	(	test897	)	;
double	ok898	=	GlobalVariableGet	(	test898	)	;
double	ok899	=	GlobalVariableGet	(	test899	)	;
double	ok900	=	GlobalVariableGet	(	test900	)	;
double	ok901	=	GlobalVariableGet	(	test901	)	;
double	ok902	=	GlobalVariableGet	(	test902	)	;
double	ok903	=	GlobalVariableGet	(	test903	)	;
double	ok904	=	GlobalVariableGet	(	test904	)	;
double	ok905	=	GlobalVariableGet	(	test905	)	;
double	ok906	=	GlobalVariableGet	(	test906	)	;
double	ok907	=	GlobalVariableGet	(	test907	)	;
double	ok908	=	GlobalVariableGet	(	test908	)	;
double	ok909	=	GlobalVariableGet	(	test909	)	;
double	ok910	=	GlobalVariableGet	(	test910	)	;
double	ok911	=	GlobalVariableGet	(	test911	)	;
double	ok912	=	GlobalVariableGet	(	test912	)	;
double	ok913	=	GlobalVariableGet	(	test913	)	;
double	ok914	=	GlobalVariableGet	(	test914	)	;
double	ok915	=	GlobalVariableGet	(	test915	)	;
double	ok916	=	GlobalVariableGet	(	test916	)	;
double	ok917	=	GlobalVariableGet	(	test917	)	;
double	ok918	=	GlobalVariableGet	(	test918	)	;
double	ok919	=	GlobalVariableGet	(	test919	)	;
double	ok920	=	GlobalVariableGet	(	test920	)	;
double	ok921	=	GlobalVariableGet	(	test921	)	;
double	ok922	=	GlobalVariableGet	(	test922	)	;
double	ok923	=	GlobalVariableGet	(	test923	)	;
double	ok924	=	GlobalVariableGet	(	test924	)	;
double	ok925	=	GlobalVariableGet	(	test925	)	;
double	ok926	=	GlobalVariableGet	(	test926	)	;
double	ok927	=	GlobalVariableGet	(	test927	)	;
double	ok928	=	GlobalVariableGet	(	test928	)	;
double	ok929	=	GlobalVariableGet	(	test929	)	;
double	ok930	=	GlobalVariableGet	(	test930	)	;
double	ok931	=	GlobalVariableGet	(	test931	)	;
double	ok932	=	GlobalVariableGet	(	test932	)	;
double	ok933	=	GlobalVariableGet	(	test933	)	;
double	ok934	=	GlobalVariableGet	(	test934	)	;
double	ok935	=	GlobalVariableGet	(	test935	)	;
double	ok936	=	GlobalVariableGet	(	test936	)	;
double	ok937	=	GlobalVariableGet	(	test937	)	;
double	ok938	=	GlobalVariableGet	(	test938	)	;
double	ok939	=	GlobalVariableGet	(	test939	)	;
double	ok940	=	GlobalVariableGet	(	test940	)	;
double	ok941	=	GlobalVariableGet	(	test941	)	;
double	ok942	=	GlobalVariableGet	(	test942	)	;
double	ok943	=	GlobalVariableGet	(	test943	)	;
double	ok944	=	GlobalVariableGet	(	test944	)	;
double	ok945	=	GlobalVariableGet	(	test945	)	;
double	ok946	=	GlobalVariableGet	(	test946	)	;
double	ok947	=	GlobalVariableGet	(	test947	)	;
double	ok948	=	GlobalVariableGet	(	test948	)	;
double	ok949	=	GlobalVariableGet	(	test949	)	;
double	ok950	=	GlobalVariableGet	(	test950	)	;
double	ok951	=	GlobalVariableGet	(	test951	)	;
double	ok952	=	GlobalVariableGet	(	test952	)	;
double	ok953	=	GlobalVariableGet	(	test953	)	;
double	ok954	=	GlobalVariableGet	(	test954	)	;
double	ok955	=	GlobalVariableGet	(	test955	)	;
double	ok956	=	GlobalVariableGet	(	test956	)	;
double	ok957	=	GlobalVariableGet	(	test957	)	;
double	ok958	=	GlobalVariableGet	(	test958	)	;
double	ok959	=	GlobalVariableGet	(	test959	)	;
double	ok960	=	GlobalVariableGet	(	test960	)	;
double	ok961	=	GlobalVariableGet	(	test961	)	;
double	ok962	=	GlobalVariableGet	(	test962	)	;
double	ok963	=	GlobalVariableGet	(	test963	)	;
double	ok964	=	GlobalVariableGet	(	test964	)	;
double	ok965	=	GlobalVariableGet	(	test965	)	;
double	ok966	=	GlobalVariableGet	(	test966	)	;
double	ok967	=	GlobalVariableGet	(	test967	)	;
double	ok968	=	GlobalVariableGet	(	test968	)	;
double	ok969	=	GlobalVariableGet	(	test969	)	;
double	ok970	=	GlobalVariableGet	(	test970	)	;
double	ok971	=	GlobalVariableGet	(	test971	)	;
double	ok972	=	GlobalVariableGet	(	test972	)	;
double	ok973	=	GlobalVariableGet	(	test973	)	;
double	ok974	=	GlobalVariableGet	(	test974	)	;
double	ok975	=	GlobalVariableGet	(	test975	)	;
double	ok976	=	GlobalVariableGet	(	test976	)	;
double	ok977	=	GlobalVariableGet	(	test977	)	;
double	ok978	=	GlobalVariableGet	(	test978	)	;
double	ok979	=	GlobalVariableGet	(	test979	)	;
double	ok980	=	GlobalVariableGet	(	test980	)	;
double	ok981	=	GlobalVariableGet	(	test981	)	;
double	ok982	=	GlobalVariableGet	(	test982	)	;
double	ok983	=	GlobalVariableGet	(	test983	)	;
double	ok984	=	GlobalVariableGet	(	test984	)	;
double	ok985	=	GlobalVariableGet	(	test985	)	;
double	ok986	=	GlobalVariableGet	(	test986	)	;
double	ok987	=	GlobalVariableGet	(	test987	)	;
double	ok988	=	GlobalVariableGet	(	test988	)	;
double	ok989	=	GlobalVariableGet	(	test989	)	;
double	ok990	=	GlobalVariableGet	(	test990	)	;
double	ok991	=	GlobalVariableGet	(	test991	)	;
double	ok992	=	GlobalVariableGet	(	test992	)	;
double	ok993	=	GlobalVariableGet	(	test993	)	;
double	ok994	=	GlobalVariableGet	(	test994	)	;
double	ok995	=	GlobalVariableGet	(	test995	)	;
double	ok996	=	GlobalVariableGet	(	test996	)	;
double	ok997	=	GlobalVariableGet	(	test997	)	;
double	ok998	=	GlobalVariableGet	(	test998	)	;
double	ok999	=	GlobalVariableGet	(	test999	)	;
double	ok1000	=	GlobalVariableGet	(	test1000	)	;

   
   buy1	=	ok1	;
buy2	=	ok2	;
buy3	=	ok3	;
buy4	=	ok4	;
buy5	=	ok5	;
buy6	=	ok6	;
buy7	=	ok7	;
buy8	=	ok8	;
buy9	=	ok9	;
buy10	=	ok10	;
buy11	=	ok11	;
buy12	=	ok12	;
buy13	=	ok13	;
buy14	=	ok14	;
buy15	=	ok15	;
buy16	=	ok16	;
buy17	=	ok17	;
buy18	=	ok18	;
buy19	=	ok19	;
buy20	=	ok20	;
buy21	=	ok21	;
buy22	=	ok22	;
buy23	=	ok23	;
buy24	=	ok24	;
buy25	=	ok25	;
buy26	=	ok26	;
buy27	=	ok27	;
buy28	=	ok28	;
buy29	=	ok29	;
buy30	=	ok30	;
buy31	=	ok31	;
buy32	=	ok32	;
buy33	=	ok33	;
buy34	=	ok34	;
buy35	=	ok35	;
buy36	=	ok36	;
buy37	=	ok37	;
buy38	=	ok38	;
buy39	=	ok39	;
buy40	=	ok40	;
buy41	=	ok41	;
buy42	=	ok42	;
buy43	=	ok43	;
buy44	=	ok44	;
buy45	=	ok45	;
buy46	=	ok46	;
buy47	=	ok47	;
buy48	=	ok48	;
buy49	=	ok49	;
buy50	=	ok50	;
buy51	=	ok51	;
buy52	=	ok52	;
buy53	=	ok53	;
buy54	=	ok54	;
buy55	=	ok55	;
buy56	=	ok56	;
buy57	=	ok57	;
buy58	=	ok58	;
buy59	=	ok59	;
buy60	=	ok60	;
buy61	=	ok61	;
buy62	=	ok62	;
buy63	=	ok63	;
buy64	=	ok64	;
buy65	=	ok65	;
buy66	=	ok66	;
buy67	=	ok67	;
buy68	=	ok68	;
buy69	=	ok69	;
buy70	=	ok70	;
buy71	=	ok71	;
buy72	=	ok72	;
buy73	=	ok73	;
buy74	=	ok74	;
buy75	=	ok75	;
buy76	=	ok76	;
buy77	=	ok77	;
buy78	=	ok78	;
buy79	=	ok79	;
buy80	=	ok80	;
buy81	=	ok81	;
buy82	=	ok82	;
buy83	=	ok83	;
buy84	=	ok84	;
buy85	=	ok85	;
buy86	=	ok86	;
buy87	=	ok87	;
buy88	=	ok88	;
buy89	=	ok89	;
buy90	=	ok90	;
buy91	=	ok91	;
buy92	=	ok92	;
buy93	=	ok93	;
buy94	=	ok94	;
buy95	=	ok95	;
buy96	=	ok96	;
buy97	=	ok97	;
buy98	=	ok98	;
buy99	=	ok99	;
buy100	=	ok100	;
buy101	=	ok101	;
buy102	=	ok102	;
buy103	=	ok103	;
buy104	=	ok104	;
buy105	=	ok105	;
buy106	=	ok106	;
buy107	=	ok107	;
buy108	=	ok108	;
buy109	=	ok109	;
buy110	=	ok110	;
buy111	=	ok111	;
buy112	=	ok112	;
buy113	=	ok113	;
buy114	=	ok114	;
buy115	=	ok115	;
buy116	=	ok116	;
buy117	=	ok117	;
buy118	=	ok118	;
buy119	=	ok119	;
buy120	=	ok120	;
buy121	=	ok121	;
buy122	=	ok122	;
buy123	=	ok123	;
buy124	=	ok124	;
buy125	=	ok125	;
buy126	=	ok126	;
buy127	=	ok127	;
buy128	=	ok128	;
buy129	=	ok129	;
buy130	=	ok130	;
buy131	=	ok131	;
buy132	=	ok132	;
buy133	=	ok133	;
buy134	=	ok134	;
buy135	=	ok135	;
buy136	=	ok136	;
buy137	=	ok137	;
buy138	=	ok138	;
buy139	=	ok139	;
buy140	=	ok140	;
buy141	=	ok141	;
buy142	=	ok142	;
buy143	=	ok143	;
buy144	=	ok144	;
buy145	=	ok145	;
buy146	=	ok146	;
buy147	=	ok147	;
buy148	=	ok148	;
buy149	=	ok149	;
buy150	=	ok150	;
buy151	=	ok151	;
buy152	=	ok152	;
buy153	=	ok153	;
buy154	=	ok154	;
buy155	=	ok155	;
buy156	=	ok156	;
buy157	=	ok157	;
buy158	=	ok158	;
buy159	=	ok159	;
buy160	=	ok160	;
buy161	=	ok161	;
buy162	=	ok162	;
buy163	=	ok163	;
buy164	=	ok164	;
buy165	=	ok165	;
buy166	=	ok166	;
buy167	=	ok167	;
buy168	=	ok168	;
buy169	=	ok169	;
buy170	=	ok170	;
buy171	=	ok171	;
buy172	=	ok172	;
buy173	=	ok173	;
buy174	=	ok174	;
buy175	=	ok175	;
buy176	=	ok176	;
buy177	=	ok177	;
buy178	=	ok178	;
buy179	=	ok179	;
buy180	=	ok180	;
buy181	=	ok181	;
buy182	=	ok182	;
buy183	=	ok183	;
buy184	=	ok184	;
buy185	=	ok185	;
buy186	=	ok186	;
buy187	=	ok187	;
buy188	=	ok188	;
buy189	=	ok189	;
buy190	=	ok190	;
buy191	=	ok191	;
buy192	=	ok192	;
buy193	=	ok193	;
buy194	=	ok194	;
buy195	=	ok195	;
buy196	=	ok196	;
buy197	=	ok197	;
buy198	=	ok198	;
buy199	=	ok199	;
buy200	=	ok200	;
buy201	=	ok201	;
buy202	=	ok202	;
buy203	=	ok203	;
buy204	=	ok204	;
buy205	=	ok205	;
buy206	=	ok206	;
buy207	=	ok207	;
buy208	=	ok208	;
buy209	=	ok209	;
buy210	=	ok210	;
buy211	=	ok211	;
buy212	=	ok212	;
buy213	=	ok213	;
buy214	=	ok214	;
buy215	=	ok215	;
buy216	=	ok216	;
buy217	=	ok217	;
buy218	=	ok218	;
buy219	=	ok219	;
buy220	=	ok220	;
buy221	=	ok221	;
buy222	=	ok222	;
buy223	=	ok223	;
buy224	=	ok224	;
buy225	=	ok225	;
buy226	=	ok226	;
buy227	=	ok227	;
buy228	=	ok228	;
buy229	=	ok229	;
buy230	=	ok230	;
buy231	=	ok231	;
buy232	=	ok232	;
buy233	=	ok233	;
buy234	=	ok234	;
buy235	=	ok235	;
buy236	=	ok236	;
buy237	=	ok237	;
buy238	=	ok238	;
buy239	=	ok239	;
buy240	=	ok240	;
buy241	=	ok241	;
buy242	=	ok242	;
buy243	=	ok243	;
buy244	=	ok244	;
buy245	=	ok245	;
buy246	=	ok246	;
buy247	=	ok247	;
buy248	=	ok248	;
buy249	=	ok249	;
buy250	=	ok250	;
buy251	=	ok251	;
buy252	=	ok252	;
buy253	=	ok253	;
buy254	=	ok254	;
buy255	=	ok255	;
buy256	=	ok256	;
buy257	=	ok257	;
buy258	=	ok258	;
buy259	=	ok259	;
buy260	=	ok260	;
buy261	=	ok261	;
buy262	=	ok262	;
buy263	=	ok263	;
buy264	=	ok264	;
buy265	=	ok265	;
buy266	=	ok266	;
buy267	=	ok267	;
buy268	=	ok268	;
buy269	=	ok269	;
buy270	=	ok270	;
buy271	=	ok271	;
buy272	=	ok272	;
buy273	=	ok273	;
buy274	=	ok274	;
buy275	=	ok275	;
buy276	=	ok276	;
buy277	=	ok277	;
buy278	=	ok278	;
buy279	=	ok279	;
buy280	=	ok280	;
buy281	=	ok281	;
buy282	=	ok282	;
buy283	=	ok283	;
buy284	=	ok284	;
buy285	=	ok285	;
buy286	=	ok286	;
buy287	=	ok287	;
buy288	=	ok288	;
buy289	=	ok289	;
buy290	=	ok290	;
buy291	=	ok291	;
buy292	=	ok292	;
buy293	=	ok293	;
buy294	=	ok294	;
buy295	=	ok295	;
buy296	=	ok296	;
buy297	=	ok297	;
buy298	=	ok298	;
buy299	=	ok299	;
buy300	=	ok300	;
buy301	=	ok301	;
buy302	=	ok302	;
buy303	=	ok303	;
buy304	=	ok304	;
buy305	=	ok305	;
buy306	=	ok306	;
buy307	=	ok307	;
buy308	=	ok308	;
buy309	=	ok309	;
buy310	=	ok310	;
buy311	=	ok311	;
buy312	=	ok312	;
buy313	=	ok313	;
buy314	=	ok314	;
buy315	=	ok315	;
buy316	=	ok316	;
buy317	=	ok317	;
buy318	=	ok318	;
buy319	=	ok319	;
buy320	=	ok320	;
buy321	=	ok321	;
buy322	=	ok322	;
buy323	=	ok323	;
buy324	=	ok324	;
buy325	=	ok325	;
buy326	=	ok326	;
buy327	=	ok327	;
buy328	=	ok328	;
buy329	=	ok329	;
buy330	=	ok330	;
buy331	=	ok331	;
buy332	=	ok332	;
buy333	=	ok333	;
buy334	=	ok334	;
buy335	=	ok335	;
buy336	=	ok336	;
buy337	=	ok337	;
buy338	=	ok338	;
buy339	=	ok339	;
buy340	=	ok340	;
buy341	=	ok341	;
buy342	=	ok342	;
buy343	=	ok343	;
buy344	=	ok344	;
buy345	=	ok345	;
buy346	=	ok346	;
buy347	=	ok347	;
buy348	=	ok348	;
buy349	=	ok349	;
buy350	=	ok350	;
buy351	=	ok351	;
buy352	=	ok352	;
buy353	=	ok353	;
buy354	=	ok354	;
buy355	=	ok355	;
buy356	=	ok356	;
buy357	=	ok357	;
buy358	=	ok358	;
buy359	=	ok359	;
buy360	=	ok360	;
buy361	=	ok361	;
buy362	=	ok362	;
buy363	=	ok363	;
buy364	=	ok364	;
buy365	=	ok365	;
buy366	=	ok366	;
buy367	=	ok367	;
buy368	=	ok368	;
buy369	=	ok369	;
buy370	=	ok370	;
buy371	=	ok371	;
buy372	=	ok372	;
buy373	=	ok373	;
buy374	=	ok374	;
buy375	=	ok375	;
buy376	=	ok376	;
buy377	=	ok377	;
buy378	=	ok378	;
buy379	=	ok379	;
buy380	=	ok380	;
buy381	=	ok381	;
buy382	=	ok382	;
buy383	=	ok383	;
buy384	=	ok384	;
buy385	=	ok385	;
buy386	=	ok386	;
buy387	=	ok387	;
buy388	=	ok388	;
buy389	=	ok389	;
buy390	=	ok390	;
buy391	=	ok391	;
buy392	=	ok392	;
buy393	=	ok393	;
buy394	=	ok394	;
buy395	=	ok395	;
buy396	=	ok396	;
buy397	=	ok397	;
buy398	=	ok398	;
buy399	=	ok399	;
buy400	=	ok400	;
buy401	=	ok401	;
buy402	=	ok402	;
buy403	=	ok403	;
buy404	=	ok404	;
buy405	=	ok405	;
buy406	=	ok406	;
buy407	=	ok407	;
buy408	=	ok408	;
buy409	=	ok409	;
buy410	=	ok410	;
buy411	=	ok411	;
buy412	=	ok412	;
buy413	=	ok413	;
buy414	=	ok414	;
buy415	=	ok415	;
buy416	=	ok416	;
buy417	=	ok417	;
buy418	=	ok418	;
buy419	=	ok419	;
buy420	=	ok420	;
buy421	=	ok421	;
buy422	=	ok422	;
buy423	=	ok423	;
buy424	=	ok424	;
buy425	=	ok425	;
buy426	=	ok426	;
buy427	=	ok427	;
buy428	=	ok428	;
buy429	=	ok429	;
buy430	=	ok430	;
buy431	=	ok431	;
buy432	=	ok432	;
buy433	=	ok433	;
buy434	=	ok434	;
buy435	=	ok435	;
buy436	=	ok436	;
buy437	=	ok437	;
buy438	=	ok438	;
buy439	=	ok439	;
buy440	=	ok440	;
buy441	=	ok441	;
buy442	=	ok442	;
buy443	=	ok443	;
buy444	=	ok444	;
buy445	=	ok445	;
buy446	=	ok446	;
buy447	=	ok447	;
buy448	=	ok448	;
buy449	=	ok449	;
buy450	=	ok450	;
buy451	=	ok451	;
buy452	=	ok452	;
buy453	=	ok453	;
buy454	=	ok454	;
buy455	=	ok455	;
buy456	=	ok456	;
buy457	=	ok457	;
buy458	=	ok458	;
buy459	=	ok459	;
buy460	=	ok460	;
buy461	=	ok461	;
buy462	=	ok462	;
buy463	=	ok463	;
buy464	=	ok464	;
buy465	=	ok465	;
buy466	=	ok466	;
buy467	=	ok467	;
buy468	=	ok468	;
buy469	=	ok469	;
buy470	=	ok470	;
buy471	=	ok471	;
buy472	=	ok472	;
buy473	=	ok473	;
buy474	=	ok474	;
buy475	=	ok475	;
buy476	=	ok476	;
buy477	=	ok477	;
buy478	=	ok478	;
buy479	=	ok479	;
buy480	=	ok480	;
buy481	=	ok481	;
buy482	=	ok482	;
buy483	=	ok483	;
buy484	=	ok484	;
buy485	=	ok485	;
buy486	=	ok486	;
buy487	=	ok487	;
buy488	=	ok488	;
buy489	=	ok489	;
buy490	=	ok490	;
buy491	=	ok491	;
buy492	=	ok492	;
buy493	=	ok493	;
buy494	=	ok494	;
buy495	=	ok495	;
buy496	=	ok496	;
buy497	=	ok497	;
buy498	=	ok498	;
buy499	=	ok499	;
buy500	=	ok500	;
buy501	=	ok501	;
buy502	=	ok502	;
buy503	=	ok503	;
buy504	=	ok504	;
buy505	=	ok505	;
buy506	=	ok506	;
buy507	=	ok507	;
buy508	=	ok508	;
buy509	=	ok509	;
buy510	=	ok510	;
buy511	=	ok511	;
buy512	=	ok512	;
buy513	=	ok513	;
buy514	=	ok514	;
buy515	=	ok515	;
buy516	=	ok516	;
buy517	=	ok517	;
buy518	=	ok518	;
buy519	=	ok519	;
buy520	=	ok520	;
buy521	=	ok521	;
buy522	=	ok522	;
buy523	=	ok523	;
buy524	=	ok524	;
buy525	=	ok525	;
buy526	=	ok526	;
buy527	=	ok527	;
buy528	=	ok528	;
buy529	=	ok529	;
buy530	=	ok530	;
buy531	=	ok531	;
buy532	=	ok532	;
buy533	=	ok533	;
buy534	=	ok534	;
buy535	=	ok535	;
buy536	=	ok536	;
buy537	=	ok537	;
buy538	=	ok538	;
buy539	=	ok539	;
buy540	=	ok540	;
buy541	=	ok541	;
buy542	=	ok542	;
buy543	=	ok543	;
buy544	=	ok544	;
buy545	=	ok545	;
buy546	=	ok546	;
buy547	=	ok547	;
buy548	=	ok548	;
buy549	=	ok549	;
buy550	=	ok550	;
buy551	=	ok551	;
buy552	=	ok552	;
buy553	=	ok553	;
buy554	=	ok554	;
buy555	=	ok555	;
buy556	=	ok556	;
buy557	=	ok557	;
buy558	=	ok558	;
buy559	=	ok559	;
buy560	=	ok560	;
buy561	=	ok561	;
buy562	=	ok562	;
buy563	=	ok563	;
buy564	=	ok564	;
buy565	=	ok565	;
buy566	=	ok566	;
buy567	=	ok567	;
buy568	=	ok568	;
buy569	=	ok569	;
buy570	=	ok570	;
buy571	=	ok571	;
buy572	=	ok572	;
buy573	=	ok573	;
buy574	=	ok574	;
buy575	=	ok575	;
buy576	=	ok576	;
buy577	=	ok577	;
buy578	=	ok578	;
buy579	=	ok579	;
buy580	=	ok580	;
buy581	=	ok581	;
buy582	=	ok582	;
buy583	=	ok583	;
buy584	=	ok584	;
buy585	=	ok585	;
buy586	=	ok586	;
buy587	=	ok587	;
buy588	=	ok588	;
buy589	=	ok589	;
buy590	=	ok590	;
buy591	=	ok591	;
buy592	=	ok592	;
buy593	=	ok593	;
buy594	=	ok594	;
buy595	=	ok595	;
buy596	=	ok596	;
buy597	=	ok597	;
buy598	=	ok598	;
buy599	=	ok599	;
buy600	=	ok600	;
buy601	=	ok601	;
buy602	=	ok602	;
buy603	=	ok603	;
buy604	=	ok604	;
buy605	=	ok605	;
buy606	=	ok606	;
buy607	=	ok607	;
buy608	=	ok608	;
buy609	=	ok609	;
buy610	=	ok610	;
buy611	=	ok611	;
buy612	=	ok612	;
buy613	=	ok613	;
buy614	=	ok614	;
buy615	=	ok615	;
buy616	=	ok616	;
buy617	=	ok617	;
buy618	=	ok618	;
buy619	=	ok619	;
buy620	=	ok620	;
buy621	=	ok621	;
buy622	=	ok622	;
buy623	=	ok623	;
buy624	=	ok624	;
buy625	=	ok625	;
buy626	=	ok626	;
buy627	=	ok627	;
buy628	=	ok628	;
buy629	=	ok629	;
buy630	=	ok630	;
buy631	=	ok631	;
buy632	=	ok632	;
buy633	=	ok633	;
buy634	=	ok634	;
buy635	=	ok635	;
buy636	=	ok636	;
buy637	=	ok637	;
buy638	=	ok638	;
buy639	=	ok639	;
buy640	=	ok640	;
buy641	=	ok641	;
buy642	=	ok642	;
buy643	=	ok643	;
buy644	=	ok644	;
buy645	=	ok645	;
buy646	=	ok646	;
buy647	=	ok647	;
buy648	=	ok648	;
buy649	=	ok649	;
buy650	=	ok650	;
buy651	=	ok651	;
buy652	=	ok652	;
buy653	=	ok653	;
buy654	=	ok654	;
buy655	=	ok655	;
buy656	=	ok656	;
buy657	=	ok657	;
buy658	=	ok658	;
buy659	=	ok659	;
buy660	=	ok660	;
buy661	=	ok661	;
buy662	=	ok662	;
buy663	=	ok663	;
buy664	=	ok664	;
buy665	=	ok665	;
buy666	=	ok666	;
buy667	=	ok667	;
buy668	=	ok668	;
buy669	=	ok669	;
buy670	=	ok670	;
buy671	=	ok671	;
buy672	=	ok672	;
buy673	=	ok673	;
buy674	=	ok674	;
buy675	=	ok675	;
buy676	=	ok676	;
buy677	=	ok677	;
buy678	=	ok678	;
buy679	=	ok679	;
buy680	=	ok680	;
buy681	=	ok681	;
buy682	=	ok682	;
buy683	=	ok683	;
buy684	=	ok684	;
buy685	=	ok685	;
buy686	=	ok686	;
buy687	=	ok687	;
buy688	=	ok688	;
buy689	=	ok689	;
buy690	=	ok690	;
buy691	=	ok691	;
buy692	=	ok692	;
buy693	=	ok693	;
buy694	=	ok694	;
buy695	=	ok695	;
buy696	=	ok696	;
buy697	=	ok697	;
buy698	=	ok698	;
buy699	=	ok699	;
buy700	=	ok700	;
buy701	=	ok701	;
buy702	=	ok702	;
buy703	=	ok703	;
buy704	=	ok704	;
buy705	=	ok705	;
buy706	=	ok706	;
buy707	=	ok707	;
buy708	=	ok708	;
buy709	=	ok709	;
buy710	=	ok710	;
buy711	=	ok711	;
buy712	=	ok712	;
buy713	=	ok713	;
buy714	=	ok714	;
buy715	=	ok715	;
buy716	=	ok716	;
buy717	=	ok717	;
buy718	=	ok718	;
buy719	=	ok719	;
buy720	=	ok720	;
buy721	=	ok721	;
buy722	=	ok722	;
buy723	=	ok723	;
buy724	=	ok724	;
buy725	=	ok725	;
buy726	=	ok726	;
buy727	=	ok727	;
buy728	=	ok728	;
buy729	=	ok729	;
buy730	=	ok730	;
buy731	=	ok731	;
buy732	=	ok732	;
buy733	=	ok733	;
buy734	=	ok734	;
buy735	=	ok735	;
buy736	=	ok736	;
buy737	=	ok737	;
buy738	=	ok738	;
buy739	=	ok739	;
buy740	=	ok740	;
buy741	=	ok741	;
buy742	=	ok742	;
buy743	=	ok743	;
buy744	=	ok744	;
buy745	=	ok745	;
buy746	=	ok746	;
buy747	=	ok747	;
buy748	=	ok748	;
buy749	=	ok749	;
buy750	=	ok750	;
buy751	=	ok751	;
buy752	=	ok752	;
buy753	=	ok753	;
buy754	=	ok754	;
buy755	=	ok755	;
buy756	=	ok756	;
buy757	=	ok757	;
buy758	=	ok758	;
buy759	=	ok759	;
buy760	=	ok760	;
buy761	=	ok761	;
buy762	=	ok762	;
buy763	=	ok763	;
buy764	=	ok764	;
buy765	=	ok765	;
buy766	=	ok766	;
buy767	=	ok767	;
buy768	=	ok768	;
buy769	=	ok769	;
buy770	=	ok770	;
buy771	=	ok771	;
buy772	=	ok772	;
buy773	=	ok773	;
buy774	=	ok774	;
buy775	=	ok775	;
buy776	=	ok776	;
buy777	=	ok777	;
buy778	=	ok778	;
buy779	=	ok779	;
buy780	=	ok780	;
buy781	=	ok781	;
buy782	=	ok782	;
buy783	=	ok783	;
buy784	=	ok784	;
buy785	=	ok785	;
buy786	=	ok786	;
buy787	=	ok787	;
buy788	=	ok788	;
buy789	=	ok789	;
buy790	=	ok790	;
buy791	=	ok791	;
buy792	=	ok792	;
buy793	=	ok793	;
buy794	=	ok794	;
buy795	=	ok795	;
buy796	=	ok796	;
buy797	=	ok797	;
buy798	=	ok798	;
buy799	=	ok799	;
buy800	=	ok800	;
buy801	=	ok801	;
buy802	=	ok802	;
buy803	=	ok803	;
buy804	=	ok804	;
buy805	=	ok805	;
buy806	=	ok806	;
buy807	=	ok807	;
buy808	=	ok808	;
buy809	=	ok809	;
buy810	=	ok810	;
buy811	=	ok811	;
buy812	=	ok812	;
buy813	=	ok813	;
buy814	=	ok814	;
buy815	=	ok815	;
buy816	=	ok816	;
buy817	=	ok817	;
buy818	=	ok818	;
buy819	=	ok819	;
buy820	=	ok820	;
buy821	=	ok821	;
buy822	=	ok822	;
buy823	=	ok823	;
buy824	=	ok824	;
buy825	=	ok825	;
buy826	=	ok826	;
buy827	=	ok827	;
buy828	=	ok828	;
buy829	=	ok829	;
buy830	=	ok830	;
buy831	=	ok831	;
buy832	=	ok832	;
buy833	=	ok833	;
buy834	=	ok834	;
buy835	=	ok835	;
buy836	=	ok836	;
buy837	=	ok837	;
buy838	=	ok838	;
buy839	=	ok839	;
buy840	=	ok840	;
buy841	=	ok841	;
buy842	=	ok842	;
buy843	=	ok843	;
buy844	=	ok844	;
buy845	=	ok845	;
buy846	=	ok846	;
buy847	=	ok847	;
buy848	=	ok848	;
buy849	=	ok849	;
buy850	=	ok850	;
buy851	=	ok851	;
buy852	=	ok852	;
buy853	=	ok853	;
buy854	=	ok854	;
buy855	=	ok855	;
buy856	=	ok856	;
buy857	=	ok857	;
buy858	=	ok858	;
buy859	=	ok859	;
buy860	=	ok860	;
buy861	=	ok861	;
buy862	=	ok862	;
buy863	=	ok863	;
buy864	=	ok864	;
buy865	=	ok865	;
buy866	=	ok866	;
buy867	=	ok867	;
buy868	=	ok868	;
buy869	=	ok869	;
buy870	=	ok870	;
buy871	=	ok871	;
buy872	=	ok872	;
buy873	=	ok873	;
buy874	=	ok874	;
buy875	=	ok875	;
buy876	=	ok876	;
buy877	=	ok877	;
buy878	=	ok878	;
buy879	=	ok879	;
buy880	=	ok880	;
buy881	=	ok881	;
buy882	=	ok882	;
buy883	=	ok883	;
buy884	=	ok884	;
buy885	=	ok885	;
buy886	=	ok886	;
buy887	=	ok887	;
buy888	=	ok888	;
buy889	=	ok889	;
buy890	=	ok890	;
buy891	=	ok891	;
buy892	=	ok892	;
buy893	=	ok893	;
buy894	=	ok894	;
buy895	=	ok895	;
buy896	=	ok896	;
buy897	=	ok897	;
buy898	=	ok898	;
buy899	=	ok899	;
buy900	=	ok900	;
buy901	=	ok901	;
buy902	=	ok902	;
buy903	=	ok903	;
buy904	=	ok904	;
buy905	=	ok905	;
buy906	=	ok906	;
buy907	=	ok907	;
buy908	=	ok908	;
buy909	=	ok909	;
buy910	=	ok910	;
buy911	=	ok911	;
buy912	=	ok912	;
buy913	=	ok913	;
buy914	=	ok914	;
buy915	=	ok915	;
buy916	=	ok916	;
buy917	=	ok917	;
buy918	=	ok918	;
buy919	=	ok919	;
buy920	=	ok920	;
buy921	=	ok921	;
buy922	=	ok922	;
buy923	=	ok923	;
buy924	=	ok924	;
buy925	=	ok925	;
buy926	=	ok926	;
buy927	=	ok927	;
buy928	=	ok928	;
buy929	=	ok929	;
buy930	=	ok930	;
buy931	=	ok931	;
buy932	=	ok932	;
buy933	=	ok933	;
buy934	=	ok934	;
buy935	=	ok935	;
buy936	=	ok936	;
buy937	=	ok937	;
buy938	=	ok938	;
buy939	=	ok939	;
buy940	=	ok940	;
buy941	=	ok941	;
buy942	=	ok942	;
buy943	=	ok943	;
buy944	=	ok944	;
buy945	=	ok945	;
buy946	=	ok946	;
buy947	=	ok947	;
buy948	=	ok948	;
buy949	=	ok949	;
buy950	=	ok950	;
buy951	=	ok951	;
buy952	=	ok952	;
buy953	=	ok953	;
buy954	=	ok954	;
buy955	=	ok955	;
buy956	=	ok956	;
buy957	=	ok957	;
buy958	=	ok958	;
buy959	=	ok959	;
buy960	=	ok960	;
buy961	=	ok961	;
buy962	=	ok962	;
buy963	=	ok963	;
buy964	=	ok964	;
buy965	=	ok965	;
buy966	=	ok966	;
buy967	=	ok967	;
buy968	=	ok968	;
buy969	=	ok969	;
buy970	=	ok970	;
buy971	=	ok971	;
buy972	=	ok972	;
buy973	=	ok973	;
buy974	=	ok974	;
buy975	=	ok975	;
buy976	=	ok976	;
buy977	=	ok977	;
buy978	=	ok978	;
buy979	=	ok979	;
buy980	=	ok980	;
buy981	=	ok981	;
buy982	=	ok982	;
buy983	=	ok983	;
buy984	=	ok984	;
buy985	=	ok985	;
buy986	=	ok986	;
buy987	=	ok987	;
buy988	=	ok988	;
buy989	=	ok989	;
buy990	=	ok990	;
buy991	=	ok991	;
buy992	=	ok992	;
buy993	=	ok993	;
buy994	=	ok994	;
buy995	=	ok995	;
buy996	=	ok996	;
buy997	=	ok997	;
buy998	=	ok998	;
buy999	=	ok999	;
buy1000	=	ok1000	;

   
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
     ObjectDelete("Average_Price_Line_"+Symbol());
   ObjectDelete("Information_"+Symbol());
//---
   
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
AdjustTrail();
CloseProfitBeforeClosingTime();
 int Total_Buy_Trades;
   double Total_Buy_Size;
   double Total_Buy_Price;
   double Buy_Profit;
//---
   int Total_Sell_Trades;
   double Total_Sell_Size;
   double Total_Sell_Price;
   double Sell_Profit;
//---
   int Net_Trades;
   double Net_Lots;
   double Net_Result;
//---
   double Average_Price;
   double distance;
   double Pip_Value=MarketInfo(Symbol(),MODE_TICKVALUE)*PipAdjust;
   double Pip_Size=MarketInfo(Symbol(),MODE_TICKSIZE)*PipAdjust;
//---
   int total=OrdersTotal();
//---
   for(int i=0;i<total;i++)
     {
      int ord=OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
        {
         if(OrderType()==OP_BUY && OrderSymbol()==Symbol())

           {
            Total_Buy_Trades++;
            Total_Buy_Price+= OrderOpenPrice()*OrderLots();
            Total_Buy_Size += OrderLots();
            Buy_Profit+=OrderProfit()+OrderSwap()+OrderCommission();
           }
         if(OrderType()==OP_SELL && OrderSymbol()==Symbol())
           {
            Total_Sell_Trades++;
            Total_Sell_Size+=OrderLots();
            Total_Sell_Price+=OrderOpenPrice()*OrderLots();
            Sell_Profit+=OrderProfit()+OrderSwap()+OrderCommission();
           }
        }
     }
   if(Total_Buy_Price>0)
     {
      Total_Buy_Price/=Total_Buy_Size;
     }
   if(Total_Sell_Price>0)
     {
      Total_Sell_Price/=Total_Sell_Size;
     }
   Net_Trades=Total_Buy_Trades+Total_Sell_Trades;
   Net_Lots=Total_Buy_Size-Total_Sell_Size;
   Net_Result=Buy_Profit+Sell_Profit;
//---
   ObjectDelete("Average_Price_Line_"+Symbol());
   ObjectDelete("Information_"+Symbol());
//---
   if(Net_Trades>0 && Net_Lots!=0)
     {
      distance=(Net_Result/(MathAbs(Net_Lots*MarketInfo(Symbol(),MODE_TICKVALUE)))*MarketInfo(Symbol(),MODE_TICKSIZE));
      if(Net_Lots>0)
        {
         Average_Price=Bid-distance;
        }
      if(Net_Lots<0)
        {
         Average_Price=Ask+distance;
        }
     }
   if(Net_Trades>0 && Net_Lots==0)
     {
      distance=(Net_Result/((MarketInfo(Symbol(),MODE_TICKVALUE)))*MarketInfo(Symbol(),MODE_TICKSIZE));
      Average_Price=Bid-distance;
     }
   ObjectDelete("Average_Price_Line_"+Symbol());
   ObjectCreate("Average_Price_Line_"+Symbol(),OBJ_HLINE,0,0,Average_Price);
   ObjectSet("Average_Price_Line_"+Symbol(),OBJPROP_WIDTH,3);
//---
   color cl=Blue;
   if(Net_Lots<0) cl=Red;
   if(Net_Lots==0) cl=White;
//---
   ObjectSet("Average_Price_Line_"+Symbol(),OBJPROP_COLOR,cl);
   ObjectCreate("Information_"+Symbol(),OBJ_LABEL,0,0,0);
//---
   int x,y;
   ChartTimePriceToXY(0,0,Time[0],Average_Price,x,y);
//---
   ObjectSet("Information_"+Symbol(),OBJPROP_XDISTANCE,220);
   ObjectSet("Information_"+Symbol(),OBJPROP_YDISTANCE,y);
   ObjectSetText("Information_"+Symbol(),"Avrg= "+DoubleToStr(Average_Price,NrOfDigits)+"  "+DoubleToStr(distance/(point),1)+" pips ("+Net_Result+" "+AccountInfoString(ACCOUNT_CURRENCY)+")  Lots= "+Net_Lots+"  Orders= "+Net_Trades,font_size,"Arial",White);
//---
  
   int BuyCnt = 0;
  int SellCnt = 0;
  int BuyStopCnt = 0;
  int SellStopCnt = 0;
  int BuyLimitCnt = 0;
  int SellLimitCnt = 0;
  
  int cnt = OrdersTotal();
  for (int i=0; i < cnt; i++) 
  {
  
 if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;
    if (OrderSymbol() != Symbol()) continue;
   // if (OrderMagicNumber() != magicnumber) continue;
 
    Comment("Open orders = "+DoubleToStr((BuyCnt+1),0) +
    "\nThe spread is "+DoubleToStr(steplevel1,2)+
    "\nThe stoplevel is "+DoubleToStr(MarketInfo(Symbol(),MODE_STOPLEVEL),2)+
   "\nTICKVALUE = " +DoubleToStr( MarketInfo(Symbol(), MODE_TICKVALUE),5)+
   "\nMargin = " +DoubleToStr (MarketInfo(Symbol(),MODE_MARGINREQUIRED)*0.01,2)+
    "\nSwap = "+DoubleToStr(MarketInfo(Symbol(),MODE_SWAPLONG),5)+
   "\nFirstopen - spread - steplevel1 = "+DoubleToStr(buy1,2));
   
    int type = OrderType();
    if (type == OP_BUY) BuyCnt++;
    if (type == OP_SELL) SellCnt++;
    if (type == OP_BUYSTOP) BuyStopCnt++;
    if (type == OP_SELLSTOP) SellStopCnt++;
    if (type == OP_BUYLIMIT) BuyLimitCnt++;
    if (type == OP_SELLLIMIT) SellLimitCnt++;
   } 
    if(BuyCnt==0)openbuy1();
   else if	(	BuyCnt==1	)	openbuy2	()	;
else if	(	BuyCnt==2	)	openbuy3	()	;
else if	(	BuyCnt==3	)	openbuy4	()	;
else if	(	BuyCnt==4	)	openbuy5	()	;
else if	(	BuyCnt==5	)	openbuy6	()	;
else if	(	BuyCnt==6	)	openbuy7	()	;
else if	(	BuyCnt==7	)	openbuy8	()	;
else if	(	BuyCnt==8	)	openbuy9	()	;
else if	(	BuyCnt==9	)	openbuy10	()	;
else if	(	BuyCnt==10	)	openbuy11	()	;
else if	(	BuyCnt==11	)	openbuy12	()	;
else if	(	BuyCnt==12	)	openbuy13	()	;
else if	(	BuyCnt==13	)	openbuy14	()	;
else if	(	BuyCnt==14	)	openbuy15	()	;
else if	(	BuyCnt==15	)	openbuy16	()	;
else if	(	BuyCnt==16	)	openbuy17	()	;
else if	(	BuyCnt==17	)	openbuy18	()	;
else if	(	BuyCnt==18	)	openbuy19	()	;
else if	(	BuyCnt==19	)	openbuy20	()	;
else if	(	BuyCnt==20	)	openbuy21	()	;
else if	(	BuyCnt==21	)	openbuy22	()	;
else if	(	BuyCnt==22	)	openbuy23	()	;
else if	(	BuyCnt==23	)	openbuy24	()	;
else if	(	BuyCnt==24	)	openbuy25	()	;
else if	(	BuyCnt==25	)	openbuy26	()	;
else if	(	BuyCnt==26	)	openbuy27	()	;
else if	(	BuyCnt==27	)	openbuy28	()	;
else if	(	BuyCnt==28	)	openbuy29	()	;
else if	(	BuyCnt==29	)	openbuy30	()	;
else if	(	BuyCnt==30	)	openbuy31	()	;
else if	(	BuyCnt==31	)	openbuy32	()	;
else if	(	BuyCnt==32	)	openbuy33	()	;
else if	(	BuyCnt==33	)	openbuy34	()	;
else if	(	BuyCnt==34	)	openbuy35	()	;
else if	(	BuyCnt==35	)	openbuy36	()	;
else if	(	BuyCnt==36	)	openbuy37	()	;
else if	(	BuyCnt==37	)	openbuy38	()	;
else if	(	BuyCnt==38	)	openbuy39	()	;
else if	(	BuyCnt==39	)	openbuy40	()	;
else if	(	BuyCnt==40	)	openbuy41	()	;
else if	(	BuyCnt==41	)	openbuy42	()	;
else if	(	BuyCnt==42	)	openbuy43	()	;
else if	(	BuyCnt==43	)	openbuy44	()	;
else if	(	BuyCnt==44	)	openbuy45	()	;
else if	(	BuyCnt==45	)	openbuy46	()	;
else if	(	BuyCnt==46	)	openbuy47	()	;
else if	(	BuyCnt==47	)	openbuy48	()	;
else if	(	BuyCnt==48	)	openbuy49	()	;
else if	(	BuyCnt==49	)	openbuy50	()	;
else if	(	BuyCnt==50	)	openbuy51	()	;
else if	(	BuyCnt==51	)	openbuy52	()	;
else if	(	BuyCnt==52	)	openbuy53	()	;
else if	(	BuyCnt==53	)	openbuy54	()	;
else if	(	BuyCnt==54	)	openbuy55	()	;
else if	(	BuyCnt==55	)	openbuy56	()	;
else if	(	BuyCnt==56	)	openbuy57	()	;
else if	(	BuyCnt==57	)	openbuy58	()	;
else if	(	BuyCnt==58	)	openbuy59	()	;
else if	(	BuyCnt==59	)	openbuy60	()	;
else if	(	BuyCnt==60	)	openbuy61	()	;
else if	(	BuyCnt==61	)	openbuy62	()	;
else if	(	BuyCnt==62	)	openbuy63	()	;
else if	(	BuyCnt==63	)	openbuy64	()	;
else if	(	BuyCnt==64	)	openbuy65	()	;
else if	(	BuyCnt==65	)	openbuy66	()	;
else if	(	BuyCnt==66	)	openbuy67	()	;
else if	(	BuyCnt==67	)	openbuy68	()	;
else if	(	BuyCnt==68	)	openbuy69	()	;
else if	(	BuyCnt==69	)	openbuy70	()	;
else if	(	BuyCnt==70	)	openbuy71	()	;
else if	(	BuyCnt==71	)	openbuy72	()	;
else if	(	BuyCnt==72	)	openbuy73	()	;
else if	(	BuyCnt==73	)	openbuy74	()	;
else if	(	BuyCnt==74	)	openbuy75	()	;
else if	(	BuyCnt==75	)	openbuy76	()	;
else if	(	BuyCnt==76	)	openbuy77	()	;
else if	(	BuyCnt==77	)	openbuy78	()	;
else if	(	BuyCnt==78	)	openbuy79	()	;
else if	(	BuyCnt==79	)	openbuy80	()	;
else if	(	BuyCnt==80	)	openbuy81	()	;
else if	(	BuyCnt==81	)	openbuy82	()	;
else if	(	BuyCnt==82	)	openbuy83	()	;
else if	(	BuyCnt==83	)	openbuy84	()	;
else if	(	BuyCnt==84	)	openbuy85	()	;
else if	(	BuyCnt==85	)	openbuy86	()	;
else if	(	BuyCnt==86	)	openbuy87	()	;
else if	(	BuyCnt==87	)	openbuy88	()	;
else if	(	BuyCnt==88	)	openbuy89	()	;
else if	(	BuyCnt==89	)	openbuy90	()	;
else if	(	BuyCnt==90	)	openbuy91	()	;
else if	(	BuyCnt==91	)	openbuy92	()	;
else if	(	BuyCnt==92	)	openbuy93	()	;
else if	(	BuyCnt==93	)	openbuy94	()	;
else if	(	BuyCnt==94	)	openbuy95	()	;
else if	(	BuyCnt==95	)	openbuy96	()	;
else if	(	BuyCnt==96	)	openbuy97	()	;
else if	(	BuyCnt==97	)	openbuy98	()	;
else if	(	BuyCnt==98	)	openbuy99	()	;
else if	(	BuyCnt==99	)	openbuy100	()	;
else if	(	BuyCnt==100	)	openbuy101	()	;
else if	(	BuyCnt==101	)	openbuy102	()	;
else if	(	BuyCnt==102	)	openbuy103	()	;
else if	(	BuyCnt==103	)	openbuy104	()	;
else if	(	BuyCnt==104	)	openbuy105	()	;
else if	(	BuyCnt==105	)	openbuy106	()	;
else if	(	BuyCnt==106	)	openbuy107	()	;
else if	(	BuyCnt==107	)	openbuy108	()	;
else if	(	BuyCnt==108	)	openbuy109	()	;
else if	(	BuyCnt==109	)	openbuy110	()	;
else if	(	BuyCnt==110	)	openbuy111	()	;
else if	(	BuyCnt==111	)	openbuy112	()	;
else if	(	BuyCnt==112	)	openbuy113	()	;
else if	(	BuyCnt==113	)	openbuy114	()	;
else if	(	BuyCnt==114	)	openbuy115	()	;
else if	(	BuyCnt==115	)	openbuy116	()	;
else if	(	BuyCnt==116	)	openbuy117	()	;
else if	(	BuyCnt==117	)	openbuy118	()	;
else if	(	BuyCnt==118	)	openbuy119	()	;
else if	(	BuyCnt==119	)	openbuy120	()	;
else if	(	BuyCnt==120	)	openbuy121	()	;
else if	(	BuyCnt==121	)	openbuy122	()	;
else if	(	BuyCnt==122	)	openbuy123	()	;
else if	(	BuyCnt==123	)	openbuy124	()	;
else if	(	BuyCnt==124	)	openbuy125	()	;
else if	(	BuyCnt==125	)	openbuy126	()	;
else if	(	BuyCnt==126	)	openbuy127	()	;
else if	(	BuyCnt==127	)	openbuy128	()	;
else if	(	BuyCnt==128	)	openbuy129	()	;
else if	(	BuyCnt==129	)	openbuy130	()	;
else if	(	BuyCnt==130	)	openbuy131	()	;
else if	(	BuyCnt==131	)	openbuy132	()	;
else if	(	BuyCnt==132	)	openbuy133	()	;
else if	(	BuyCnt==133	)	openbuy134	()	;
else if	(	BuyCnt==134	)	openbuy135	()	;
else if	(	BuyCnt==135	)	openbuy136	()	;
else if	(	BuyCnt==136	)	openbuy137	()	;
else if	(	BuyCnt==137	)	openbuy138	()	;
else if	(	BuyCnt==138	)	openbuy139	()	;
else if	(	BuyCnt==139	)	openbuy140	()	;
else if	(	BuyCnt==140	)	openbuy141	()	;
else if	(	BuyCnt==141	)	openbuy142	()	;
else if	(	BuyCnt==142	)	openbuy143	()	;
else if	(	BuyCnt==143	)	openbuy144	()	;
else if	(	BuyCnt==144	)	openbuy145	()	;
else if	(	BuyCnt==145	)	openbuy146	()	;
else if	(	BuyCnt==146	)	openbuy147	()	;
else if	(	BuyCnt==147	)	openbuy148	()	;
else if	(	BuyCnt==148	)	openbuy149	()	;
else if	(	BuyCnt==149	)	openbuy150	()	;
else if	(	BuyCnt==150	)	openbuy151	()	;
else if	(	BuyCnt==151	)	openbuy152	()	;
else if	(	BuyCnt==152	)	openbuy153	()	;
else if	(	BuyCnt==153	)	openbuy154	()	;
else if	(	BuyCnt==154	)	openbuy155	()	;
else if	(	BuyCnt==155	)	openbuy156	()	;
else if	(	BuyCnt==156	)	openbuy157	()	;
else if	(	BuyCnt==157	)	openbuy158	()	;
else if	(	BuyCnt==158	)	openbuy159	()	;
else if	(	BuyCnt==159	)	openbuy160	()	;
else if	(	BuyCnt==160	)	openbuy161	()	;
else if	(	BuyCnt==161	)	openbuy162	()	;
else if	(	BuyCnt==162	)	openbuy163	()	;
else if	(	BuyCnt==163	)	openbuy164	()	;
else if	(	BuyCnt==164	)	openbuy165	()	;
else if	(	BuyCnt==165	)	openbuy166	()	;
else if	(	BuyCnt==166	)	openbuy167	()	;
else if	(	BuyCnt==167	)	openbuy168	()	;
else if	(	BuyCnt==168	)	openbuy169	()	;
else if	(	BuyCnt==169	)	openbuy170	()	;
else if	(	BuyCnt==170	)	openbuy171	()	;
else if	(	BuyCnt==171	)	openbuy172	()	;
else if	(	BuyCnt==172	)	openbuy173	()	;
else if	(	BuyCnt==173	)	openbuy174	()	;
else if	(	BuyCnt==174	)	openbuy175	()	;
else if	(	BuyCnt==175	)	openbuy176	()	;
else if	(	BuyCnt==176	)	openbuy177	()	;
else if	(	BuyCnt==177	)	openbuy178	()	;
else if	(	BuyCnt==178	)	openbuy179	()	;
else if	(	BuyCnt==179	)	openbuy180	()	;
else if	(	BuyCnt==180	)	openbuy181	()	;
else if	(	BuyCnt==181	)	openbuy182	()	;
else if	(	BuyCnt==182	)	openbuy183	()	;
else if	(	BuyCnt==183	)	openbuy184	()	;
else if	(	BuyCnt==184	)	openbuy185	()	;
else if	(	BuyCnt==185	)	openbuy186	()	;
else if	(	BuyCnt==186	)	openbuy187	()	;
else if	(	BuyCnt==187	)	openbuy188	()	;
else if	(	BuyCnt==188	)	openbuy189	()	;
else if	(	BuyCnt==189	)	openbuy190	()	;
else if	(	BuyCnt==190	)	openbuy191	()	;
else if	(	BuyCnt==191	)	openbuy192	()	;
else if	(	BuyCnt==192	)	openbuy193	()	;
else if	(	BuyCnt==193	)	openbuy194	()	;
else if	(	BuyCnt==194	)	openbuy195	()	;
else if	(	BuyCnt==195	)	openbuy196	()	;
else if	(	BuyCnt==196	)	openbuy197	()	;
else if	(	BuyCnt==197	)	openbuy198	()	;
else if	(	BuyCnt==198	)	openbuy199	()	;
else if	(	BuyCnt==199	)	openbuy200	()	;
else if	(	BuyCnt==200	)	openbuy201	()	;
else if	(	BuyCnt==201	)	openbuy202	()	;
else if	(	BuyCnt==202	)	openbuy203	()	;
else if	(	BuyCnt==203	)	openbuy204	()	;
else if	(	BuyCnt==204	)	openbuy205	()	;
else if	(	BuyCnt==205	)	openbuy206	()	;
else if	(	BuyCnt==206	)	openbuy207	()	;
else if	(	BuyCnt==207	)	openbuy208	()	;
else if	(	BuyCnt==208	)	openbuy209	()	;
else if	(	BuyCnt==209	)	openbuy210	()	;
else if	(	BuyCnt==210	)	openbuy211	()	;
else if	(	BuyCnt==211	)	openbuy212	()	;
else if	(	BuyCnt==212	)	openbuy213	()	;
else if	(	BuyCnt==213	)	openbuy214	()	;
else if	(	BuyCnt==214	)	openbuy215	()	;
else if	(	BuyCnt==215	)	openbuy216	()	;
else if	(	BuyCnt==216	)	openbuy217	()	;
else if	(	BuyCnt==217	)	openbuy218	()	;
else if	(	BuyCnt==218	)	openbuy219	()	;
else if	(	BuyCnt==219	)	openbuy220	()	;
else if	(	BuyCnt==220	)	openbuy221	()	;
else if	(	BuyCnt==221	)	openbuy222	()	;
else if	(	BuyCnt==222	)	openbuy223	()	;
else if	(	BuyCnt==223	)	openbuy224	()	;
else if	(	BuyCnt==224	)	openbuy225	()	;
else if	(	BuyCnt==225	)	openbuy226	()	;
else if	(	BuyCnt==226	)	openbuy227	()	;
else if	(	BuyCnt==227	)	openbuy228	()	;
else if	(	BuyCnt==228	)	openbuy229	()	;
else if	(	BuyCnt==229	)	openbuy230	()	;
else if	(	BuyCnt==230	)	openbuy231	()	;
else if	(	BuyCnt==231	)	openbuy232	()	;
else if	(	BuyCnt==232	)	openbuy233	()	;
else if	(	BuyCnt==233	)	openbuy234	()	;
else if	(	BuyCnt==234	)	openbuy235	()	;
else if	(	BuyCnt==235	)	openbuy236	()	;
else if	(	BuyCnt==236	)	openbuy237	()	;
else if	(	BuyCnt==237	)	openbuy238	()	;
else if	(	BuyCnt==238	)	openbuy239	()	;
else if	(	BuyCnt==239	)	openbuy240	()	;
else if	(	BuyCnt==240	)	openbuy241	()	;
else if	(	BuyCnt==241	)	openbuy242	()	;
else if	(	BuyCnt==242	)	openbuy243	()	;
else if	(	BuyCnt==243	)	openbuy244	()	;
else if	(	BuyCnt==244	)	openbuy245	()	;
else if	(	BuyCnt==245	)	openbuy246	()	;
else if	(	BuyCnt==246	)	openbuy247	()	;
else if	(	BuyCnt==247	)	openbuy248	()	;
else if	(	BuyCnt==248	)	openbuy249	()	;
else if	(	BuyCnt==249	)	openbuy250	()	;
else if	(	BuyCnt==250	)	openbuy251	()	;
else if	(	BuyCnt==251	)	openbuy252	()	;
else if	(	BuyCnt==252	)	openbuy253	()	;
else if	(	BuyCnt==253	)	openbuy254	()	;
else if	(	BuyCnt==254	)	openbuy255	()	;
else if	(	BuyCnt==255	)	openbuy256	()	;
else if	(	BuyCnt==256	)	openbuy257	()	;
else if	(	BuyCnt==257	)	openbuy258	()	;
else if	(	BuyCnt==258	)	openbuy259	()	;
else if	(	BuyCnt==259	)	openbuy260	()	;
else if	(	BuyCnt==260	)	openbuy261	()	;
else if	(	BuyCnt==261	)	openbuy262	()	;
else if	(	BuyCnt==262	)	openbuy263	()	;
else if	(	BuyCnt==263	)	openbuy264	()	;
else if	(	BuyCnt==264	)	openbuy265	()	;
else if	(	BuyCnt==265	)	openbuy266	()	;
else if	(	BuyCnt==266	)	openbuy267	()	;
else if	(	BuyCnt==267	)	openbuy268	()	;
else if	(	BuyCnt==268	)	openbuy269	()	;
else if	(	BuyCnt==269	)	openbuy270	()	;
else if	(	BuyCnt==270	)	openbuy271	()	;
else if	(	BuyCnt==271	)	openbuy272	()	;
else if	(	BuyCnt==272	)	openbuy273	()	;
else if	(	BuyCnt==273	)	openbuy274	()	;
else if	(	BuyCnt==274	)	openbuy275	()	;
else if	(	BuyCnt==275	)	openbuy276	()	;
else if	(	BuyCnt==276	)	openbuy277	()	;
else if	(	BuyCnt==277	)	openbuy278	()	;
else if	(	BuyCnt==278	)	openbuy279	()	;
else if	(	BuyCnt==279	)	openbuy280	()	;
else if	(	BuyCnt==280	)	openbuy281	()	;
else if	(	BuyCnt==281	)	openbuy282	()	;
else if	(	BuyCnt==282	)	openbuy283	()	;
else if	(	BuyCnt==283	)	openbuy284	()	;
else if	(	BuyCnt==284	)	openbuy285	()	;
else if	(	BuyCnt==285	)	openbuy286	()	;
else if	(	BuyCnt==286	)	openbuy287	()	;
else if	(	BuyCnt==287	)	openbuy288	()	;
else if	(	BuyCnt==288	)	openbuy289	()	;
else if	(	BuyCnt==289	)	openbuy290	()	;
else if	(	BuyCnt==290	)	openbuy291	()	;
else if	(	BuyCnt==291	)	openbuy292	()	;
else if	(	BuyCnt==292	)	openbuy293	()	;
else if	(	BuyCnt==293	)	openbuy294	()	;
else if	(	BuyCnt==294	)	openbuy295	()	;
else if	(	BuyCnt==295	)	openbuy296	()	;
else if	(	BuyCnt==296	)	openbuy297	()	;
else if	(	BuyCnt==297	)	openbuy298	()	;
else if	(	BuyCnt==298	)	openbuy299	()	;
else if	(	BuyCnt==299	)	openbuy300	()	;
else if	(	BuyCnt==300	)	openbuy301	()	;
else if	(	BuyCnt==301	)	openbuy302	()	;
else if	(	BuyCnt==302	)	openbuy303	()	;
else if	(	BuyCnt==303	)	openbuy304	()	;
else if	(	BuyCnt==304	)	openbuy305	()	;
else if	(	BuyCnt==305	)	openbuy306	()	;
else if	(	BuyCnt==306	)	openbuy307	()	;
else if	(	BuyCnt==307	)	openbuy308	()	;
else if	(	BuyCnt==308	)	openbuy309	()	;
else if	(	BuyCnt==309	)	openbuy310	()	;
else if	(	BuyCnt==310	)	openbuy311	()	;
else if	(	BuyCnt==311	)	openbuy312	()	;
else if	(	BuyCnt==312	)	openbuy313	()	;
else if	(	BuyCnt==313	)	openbuy314	()	;
else if	(	BuyCnt==314	)	openbuy315	()	;
else if	(	BuyCnt==315	)	openbuy316	()	;
else if	(	BuyCnt==316	)	openbuy317	()	;
else if	(	BuyCnt==317	)	openbuy318	()	;
else if	(	BuyCnt==318	)	openbuy319	()	;
else if	(	BuyCnt==319	)	openbuy320	()	;
else if	(	BuyCnt==320	)	openbuy321	()	;
else if	(	BuyCnt==321	)	openbuy322	()	;
else if	(	BuyCnt==322	)	openbuy323	()	;
else if	(	BuyCnt==323	)	openbuy324	()	;
else if	(	BuyCnt==324	)	openbuy325	()	;
else if	(	BuyCnt==325	)	openbuy326	()	;
else if	(	BuyCnt==326	)	openbuy327	()	;
else if	(	BuyCnt==327	)	openbuy328	()	;
else if	(	BuyCnt==328	)	openbuy329	()	;
else if	(	BuyCnt==329	)	openbuy330	()	;
else if	(	BuyCnt==330	)	openbuy331	()	;
else if	(	BuyCnt==331	)	openbuy332	()	;
else if	(	BuyCnt==332	)	openbuy333	()	;
else if	(	BuyCnt==333	)	openbuy334	()	;
else if	(	BuyCnt==334	)	openbuy335	()	;
else if	(	BuyCnt==335	)	openbuy336	()	;
else if	(	BuyCnt==336	)	openbuy337	()	;
else if	(	BuyCnt==337	)	openbuy338	()	;
else if	(	BuyCnt==338	)	openbuy339	()	;
else if	(	BuyCnt==339	)	openbuy340	()	;
else if	(	BuyCnt==340	)	openbuy341	()	;
else if	(	BuyCnt==341	)	openbuy342	()	;
else if	(	BuyCnt==342	)	openbuy343	()	;
else if	(	BuyCnt==343	)	openbuy344	()	;
else if	(	BuyCnt==344	)	openbuy345	()	;
else if	(	BuyCnt==345	)	openbuy346	()	;
else if	(	BuyCnt==346	)	openbuy347	()	;
else if	(	BuyCnt==347	)	openbuy348	()	;
else if	(	BuyCnt==348	)	openbuy349	()	;
else if	(	BuyCnt==349	)	openbuy350	()	;
else if	(	BuyCnt==350	)	openbuy351	()	;
else if	(	BuyCnt==351	)	openbuy352	()	;
else if	(	BuyCnt==352	)	openbuy353	()	;
else if	(	BuyCnt==353	)	openbuy354	()	;
else if	(	BuyCnt==354	)	openbuy355	()	;
else if	(	BuyCnt==355	)	openbuy356	()	;
else if	(	BuyCnt==356	)	openbuy357	()	;
else if	(	BuyCnt==357	)	openbuy358	()	;
else if	(	BuyCnt==358	)	openbuy359	()	;
else if	(	BuyCnt==359	)	openbuy360	()	;
else if	(	BuyCnt==360	)	openbuy361	()	;
else if	(	BuyCnt==361	)	openbuy362	()	;
else if	(	BuyCnt==362	)	openbuy363	()	;
else if	(	BuyCnt==363	)	openbuy364	()	;
else if	(	BuyCnt==364	)	openbuy365	()	;
else if	(	BuyCnt==365	)	openbuy366	()	;
else if	(	BuyCnt==366	)	openbuy367	()	;
else if	(	BuyCnt==367	)	openbuy368	()	;
else if	(	BuyCnt==368	)	openbuy369	()	;
else if	(	BuyCnt==369	)	openbuy370	()	;
else if	(	BuyCnt==370	)	openbuy371	()	;
else if	(	BuyCnt==371	)	openbuy372	()	;
else if	(	BuyCnt==372	)	openbuy373	()	;
else if	(	BuyCnt==373	)	openbuy374	()	;
else if	(	BuyCnt==374	)	openbuy375	()	;
else if	(	BuyCnt==375	)	openbuy376	()	;
else if	(	BuyCnt==376	)	openbuy377	()	;
else if	(	BuyCnt==377	)	openbuy378	()	;
else if	(	BuyCnt==378	)	openbuy379	()	;
else if	(	BuyCnt==379	)	openbuy380	()	;
else if	(	BuyCnt==380	)	openbuy381	()	;
else if	(	BuyCnt==381	)	openbuy382	()	;
else if	(	BuyCnt==382	)	openbuy383	()	;
else if	(	BuyCnt==383	)	openbuy384	()	;
else if	(	BuyCnt==384	)	openbuy385	()	;
else if	(	BuyCnt==385	)	openbuy386	()	;
else if	(	BuyCnt==386	)	openbuy387	()	;
else if	(	BuyCnt==387	)	openbuy388	()	;
else if	(	BuyCnt==388	)	openbuy389	()	;
else if	(	BuyCnt==389	)	openbuy390	()	;
else if	(	BuyCnt==390	)	openbuy391	()	;
else if	(	BuyCnt==391	)	openbuy392	()	;
else if	(	BuyCnt==392	)	openbuy393	()	;
else if	(	BuyCnt==393	)	openbuy394	()	;
else if	(	BuyCnt==394	)	openbuy395	()	;
else if	(	BuyCnt==395	)	openbuy396	()	;
else if	(	BuyCnt==396	)	openbuy397	()	;
else if	(	BuyCnt==397	)	openbuy398	()	;
else if	(	BuyCnt==398	)	openbuy399	()	;
else if	(	BuyCnt==399	)	openbuy400	()	;
else if	(	BuyCnt==400	)	openbuy401	()	;
else if	(	BuyCnt==401	)	openbuy402	()	;
else if	(	BuyCnt==402	)	openbuy403	()	;
else if	(	BuyCnt==403	)	openbuy404	()	;
else if	(	BuyCnt==404	)	openbuy405	()	;
else if	(	BuyCnt==405	)	openbuy406	()	;
else if	(	BuyCnt==406	)	openbuy407	()	;
else if	(	BuyCnt==407	)	openbuy408	()	;
else if	(	BuyCnt==408	)	openbuy409	()	;
else if	(	BuyCnt==409	)	openbuy410	()	;
else if	(	BuyCnt==410	)	openbuy411	()	;
else if	(	BuyCnt==411	)	openbuy412	()	;
else if	(	BuyCnt==412	)	openbuy413	()	;
else if	(	BuyCnt==413	)	openbuy414	()	;
else if	(	BuyCnt==414	)	openbuy415	()	;
else if	(	BuyCnt==415	)	openbuy416	()	;
else if	(	BuyCnt==416	)	openbuy417	()	;
else if	(	BuyCnt==417	)	openbuy418	()	;
else if	(	BuyCnt==418	)	openbuy419	()	;
else if	(	BuyCnt==419	)	openbuy420	()	;
else if	(	BuyCnt==420	)	openbuy421	()	;
else if	(	BuyCnt==421	)	openbuy422	()	;
else if	(	BuyCnt==422	)	openbuy423	()	;
else if	(	BuyCnt==423	)	openbuy424	()	;
else if	(	BuyCnt==424	)	openbuy425	()	;
else if	(	BuyCnt==425	)	openbuy426	()	;
else if	(	BuyCnt==426	)	openbuy427	()	;
else if	(	BuyCnt==427	)	openbuy428	()	;
else if	(	BuyCnt==428	)	openbuy429	()	;
else if	(	BuyCnt==429	)	openbuy430	()	;
else if	(	BuyCnt==430	)	openbuy431	()	;
else if	(	BuyCnt==431	)	openbuy432	()	;
else if	(	BuyCnt==432	)	openbuy433	()	;
else if	(	BuyCnt==433	)	openbuy434	()	;
else if	(	BuyCnt==434	)	openbuy435	()	;
else if	(	BuyCnt==435	)	openbuy436	()	;
else if	(	BuyCnt==436	)	openbuy437	()	;
else if	(	BuyCnt==437	)	openbuy438	()	;
else if	(	BuyCnt==438	)	openbuy439	()	;
else if	(	BuyCnt==439	)	openbuy440	()	;
else if	(	BuyCnt==440	)	openbuy441	()	;
else if	(	BuyCnt==441	)	openbuy442	()	;
else if	(	BuyCnt==442	)	openbuy443	()	;
else if	(	BuyCnt==443	)	openbuy444	()	;
else if	(	BuyCnt==444	)	openbuy445	()	;
else if	(	BuyCnt==445	)	openbuy446	()	;
else if	(	BuyCnt==446	)	openbuy447	()	;
else if	(	BuyCnt==447	)	openbuy448	()	;
else if	(	BuyCnt==448	)	openbuy449	()	;
else if	(	BuyCnt==449	)	openbuy450	()	;
else if	(	BuyCnt==450	)	openbuy451	()	;
else if	(	BuyCnt==451	)	openbuy452	()	;
else if	(	BuyCnt==452	)	openbuy453	()	;
else if	(	BuyCnt==453	)	openbuy454	()	;
else if	(	BuyCnt==454	)	openbuy455	()	;
else if	(	BuyCnt==455	)	openbuy456	()	;
else if	(	BuyCnt==456	)	openbuy457	()	;
else if	(	BuyCnt==457	)	openbuy458	()	;
else if	(	BuyCnt==458	)	openbuy459	()	;
else if	(	BuyCnt==459	)	openbuy460	()	;
else if	(	BuyCnt==460	)	openbuy461	()	;
else if	(	BuyCnt==461	)	openbuy462	()	;
else if	(	BuyCnt==462	)	openbuy463	()	;
else if	(	BuyCnt==463	)	openbuy464	()	;
else if	(	BuyCnt==464	)	openbuy465	()	;
else if	(	BuyCnt==465	)	openbuy466	()	;
else if	(	BuyCnt==466	)	openbuy467	()	;
else if	(	BuyCnt==467	)	openbuy468	()	;
else if	(	BuyCnt==468	)	openbuy469	()	;
else if	(	BuyCnt==469	)	openbuy470	()	;
else if	(	BuyCnt==470	)	openbuy471	()	;
else if	(	BuyCnt==471	)	openbuy472	()	;
else if	(	BuyCnt==472	)	openbuy473	()	;
else if	(	BuyCnt==473	)	openbuy474	()	;
else if	(	BuyCnt==474	)	openbuy475	()	;
else if	(	BuyCnt==475	)	openbuy476	()	;
else if	(	BuyCnt==476	)	openbuy477	()	;
else if	(	BuyCnt==477	)	openbuy478	()	;
else if	(	BuyCnt==478	)	openbuy479	()	;
else if	(	BuyCnt==479	)	openbuy480	()	;
else if	(	BuyCnt==480	)	openbuy481	()	;
else if	(	BuyCnt==481	)	openbuy482	()	;
else if	(	BuyCnt==482	)	openbuy483	()	;
else if	(	BuyCnt==483	)	openbuy484	()	;
else if	(	BuyCnt==484	)	openbuy485	()	;
else if	(	BuyCnt==485	)	openbuy486	()	;
else if	(	BuyCnt==486	)	openbuy487	()	;
else if	(	BuyCnt==487	)	openbuy488	()	;
else if	(	BuyCnt==488	)	openbuy489	()	;
else if	(	BuyCnt==489	)	openbuy490	()	;
else if	(	BuyCnt==490	)	openbuy491	()	;
else if	(	BuyCnt==491	)	openbuy492	()	;
else if	(	BuyCnt==492	)	openbuy493	()	;
else if	(	BuyCnt==493	)	openbuy494	()	;
else if	(	BuyCnt==494	)	openbuy495	()	;
else if	(	BuyCnt==495	)	openbuy496	()	;
else if	(	BuyCnt==496	)	openbuy497	()	;
else if	(	BuyCnt==497	)	openbuy498	()	;
else if	(	BuyCnt==498	)	openbuy499	()	;
else if	(	BuyCnt==499	)	openbuy500	()	;
else if	(	BuyCnt==500	)	openbuy501	()	;
else if	(	BuyCnt==501	)	openbuy502	()	;
else if	(	BuyCnt==502	)	openbuy503	()	;
else if	(	BuyCnt==503	)	openbuy504	()	;
else if	(	BuyCnt==504	)	openbuy505	()	;
else if	(	BuyCnt==505	)	openbuy506	()	;
else if	(	BuyCnt==506	)	openbuy507	()	;
else if	(	BuyCnt==507	)	openbuy508	()	;
else if	(	BuyCnt==508	)	openbuy509	()	;
else if	(	BuyCnt==509	)	openbuy510	()	;
else if	(	BuyCnt==510	)	openbuy511	()	;
else if	(	BuyCnt==511	)	openbuy512	()	;
else if	(	BuyCnt==512	)	openbuy513	()	;
else if	(	BuyCnt==513	)	openbuy514	()	;
else if	(	BuyCnt==514	)	openbuy515	()	;
else if	(	BuyCnt==515	)	openbuy516	()	;
else if	(	BuyCnt==516	)	openbuy517	()	;
else if	(	BuyCnt==517	)	openbuy518	()	;
else if	(	BuyCnt==518	)	openbuy519	()	;
else if	(	BuyCnt==519	)	openbuy520	()	;
else if	(	BuyCnt==520	)	openbuy521	()	;
else if	(	BuyCnt==521	)	openbuy522	()	;
else if	(	BuyCnt==522	)	openbuy523	()	;
else if	(	BuyCnt==523	)	openbuy524	()	;
else if	(	BuyCnt==524	)	openbuy525	()	;
else if	(	BuyCnt==525	)	openbuy526	()	;
else if	(	BuyCnt==526	)	openbuy527	()	;
else if	(	BuyCnt==527	)	openbuy528	()	;
else if	(	BuyCnt==528	)	openbuy529	()	;
else if	(	BuyCnt==529	)	openbuy530	()	;
else if	(	BuyCnt==530	)	openbuy531	()	;
else if	(	BuyCnt==531	)	openbuy532	()	;
else if	(	BuyCnt==532	)	openbuy533	()	;
else if	(	BuyCnt==533	)	openbuy534	()	;
else if	(	BuyCnt==534	)	openbuy535	()	;
else if	(	BuyCnt==535	)	openbuy536	()	;
else if	(	BuyCnt==536	)	openbuy537	()	;
else if	(	BuyCnt==537	)	openbuy538	()	;
else if	(	BuyCnt==538	)	openbuy539	()	;
else if	(	BuyCnt==539	)	openbuy540	()	;
else if	(	BuyCnt==540	)	openbuy541	()	;
else if	(	BuyCnt==541	)	openbuy542	()	;
else if	(	BuyCnt==542	)	openbuy543	()	;
else if	(	BuyCnt==543	)	openbuy544	()	;
else if	(	BuyCnt==544	)	openbuy545	()	;
else if	(	BuyCnt==545	)	openbuy546	()	;
else if	(	BuyCnt==546	)	openbuy547	()	;
else if	(	BuyCnt==547	)	openbuy548	()	;
else if	(	BuyCnt==548	)	openbuy549	()	;
else if	(	BuyCnt==549	)	openbuy550	()	;
else if	(	BuyCnt==550	)	openbuy551	()	;
else if	(	BuyCnt==551	)	openbuy552	()	;
else if	(	BuyCnt==552	)	openbuy553	()	;
else if	(	BuyCnt==553	)	openbuy554	()	;
else if	(	BuyCnt==554	)	openbuy555	()	;
else if	(	BuyCnt==555	)	openbuy556	()	;
else if	(	BuyCnt==556	)	openbuy557	()	;
else if	(	BuyCnt==557	)	openbuy558	()	;
else if	(	BuyCnt==558	)	openbuy559	()	;
else if	(	BuyCnt==559	)	openbuy560	()	;
else if	(	BuyCnt==560	)	openbuy561	()	;
else if	(	BuyCnt==561	)	openbuy562	()	;
else if	(	BuyCnt==562	)	openbuy563	()	;
else if	(	BuyCnt==563	)	openbuy564	()	;
else if	(	BuyCnt==564	)	openbuy565	()	;
else if	(	BuyCnt==565	)	openbuy566	()	;
else if	(	BuyCnt==566	)	openbuy567	()	;
else if	(	BuyCnt==567	)	openbuy568	()	;
else if	(	BuyCnt==568	)	openbuy569	()	;
else if	(	BuyCnt==569	)	openbuy570	()	;
else if	(	BuyCnt==570	)	openbuy571	()	;
else if	(	BuyCnt==571	)	openbuy572	()	;
else if	(	BuyCnt==572	)	openbuy573	()	;
else if	(	BuyCnt==573	)	openbuy574	()	;
else if	(	BuyCnt==574	)	openbuy575	()	;
else if	(	BuyCnt==575	)	openbuy576	()	;
else if	(	BuyCnt==576	)	openbuy577	()	;
else if	(	BuyCnt==577	)	openbuy578	()	;
else if	(	BuyCnt==578	)	openbuy579	()	;
else if	(	BuyCnt==579	)	openbuy580	()	;
else if	(	BuyCnt==580	)	openbuy581	()	;
else if	(	BuyCnt==581	)	openbuy582	()	;
else if	(	BuyCnt==582	)	openbuy583	()	;
else if	(	BuyCnt==583	)	openbuy584	()	;
else if	(	BuyCnt==584	)	openbuy585	()	;
else if	(	BuyCnt==585	)	openbuy586	()	;
else if	(	BuyCnt==586	)	openbuy587	()	;
else if	(	BuyCnt==587	)	openbuy588	()	;
else if	(	BuyCnt==588	)	openbuy589	()	;
else if	(	BuyCnt==589	)	openbuy590	()	;
else if	(	BuyCnt==590	)	openbuy591	()	;
else if	(	BuyCnt==591	)	openbuy592	()	;
else if	(	BuyCnt==592	)	openbuy593	()	;
else if	(	BuyCnt==593	)	openbuy594	()	;
else if	(	BuyCnt==594	)	openbuy595	()	;
else if	(	BuyCnt==595	)	openbuy596	()	;
else if	(	BuyCnt==596	)	openbuy597	()	;
else if	(	BuyCnt==597	)	openbuy598	()	;
else if	(	BuyCnt==598	)	openbuy599	()	;
else if	(	BuyCnt==599	)	openbuy600	()	;
else if	(	BuyCnt==600	)	openbuy601	()	;
else if	(	BuyCnt==601	)	openbuy602	()	;
else if	(	BuyCnt==602	)	openbuy603	()	;
else if	(	BuyCnt==603	)	openbuy604	()	;
else if	(	BuyCnt==604	)	openbuy605	()	;
else if	(	BuyCnt==605	)	openbuy606	()	;
else if	(	BuyCnt==606	)	openbuy607	()	;
else if	(	BuyCnt==607	)	openbuy608	()	;
else if	(	BuyCnt==608	)	openbuy609	()	;
else if	(	BuyCnt==609	)	openbuy610	()	;
else if	(	BuyCnt==610	)	openbuy611	()	;
else if	(	BuyCnt==611	)	openbuy612	()	;
else if	(	BuyCnt==612	)	openbuy613	()	;
else if	(	BuyCnt==613	)	openbuy614	()	;
else if	(	BuyCnt==614	)	openbuy615	()	;
else if	(	BuyCnt==615	)	openbuy616	()	;
else if	(	BuyCnt==616	)	openbuy617	()	;
else if	(	BuyCnt==617	)	openbuy618	()	;
else if	(	BuyCnt==618	)	openbuy619	()	;
else if	(	BuyCnt==619	)	openbuy620	()	;
else if	(	BuyCnt==620	)	openbuy621	()	;
else if	(	BuyCnt==621	)	openbuy622	()	;
else if	(	BuyCnt==622	)	openbuy623	()	;
else if	(	BuyCnt==623	)	openbuy624	()	;
else if	(	BuyCnt==624	)	openbuy625	()	;
else if	(	BuyCnt==625	)	openbuy626	()	;
else if	(	BuyCnt==626	)	openbuy627	()	;
else if	(	BuyCnt==627	)	openbuy628	()	;
else if	(	BuyCnt==628	)	openbuy629	()	;
else if	(	BuyCnt==629	)	openbuy630	()	;
else if	(	BuyCnt==630	)	openbuy631	()	;
else if	(	BuyCnt==631	)	openbuy632	()	;
else if	(	BuyCnt==632	)	openbuy633	()	;
else if	(	BuyCnt==633	)	openbuy634	()	;
else if	(	BuyCnt==634	)	openbuy635	()	;
else if	(	BuyCnt==635	)	openbuy636	()	;
else if	(	BuyCnt==636	)	openbuy637	()	;
else if	(	BuyCnt==637	)	openbuy638	()	;
else if	(	BuyCnt==638	)	openbuy639	()	;
else if	(	BuyCnt==639	)	openbuy640	()	;
else if	(	BuyCnt==640	)	openbuy641	()	;
else if	(	BuyCnt==641	)	openbuy642	()	;
else if	(	BuyCnt==642	)	openbuy643	()	;
else if	(	BuyCnt==643	)	openbuy644	()	;
else if	(	BuyCnt==644	)	openbuy645	()	;
else if	(	BuyCnt==645	)	openbuy646	()	;
else if	(	BuyCnt==646	)	openbuy647	()	;
else if	(	BuyCnt==647	)	openbuy648	()	;
else if	(	BuyCnt==648	)	openbuy649	()	;
else if	(	BuyCnt==649	)	openbuy650	()	;
else if	(	BuyCnt==650	)	openbuy651	()	;
else if	(	BuyCnt==651	)	openbuy652	()	;
else if	(	BuyCnt==652	)	openbuy653	()	;
else if	(	BuyCnt==653	)	openbuy654	()	;
else if	(	BuyCnt==654	)	openbuy655	()	;
else if	(	BuyCnt==655	)	openbuy656	()	;
else if	(	BuyCnt==656	)	openbuy657	()	;
else if	(	BuyCnt==657	)	openbuy658	()	;
else if	(	BuyCnt==658	)	openbuy659	()	;
else if	(	BuyCnt==659	)	openbuy660	()	;
else if	(	BuyCnt==660	)	openbuy661	()	;
else if	(	BuyCnt==661	)	openbuy662	()	;
else if	(	BuyCnt==662	)	openbuy663	()	;
else if	(	BuyCnt==663	)	openbuy664	()	;
else if	(	BuyCnt==664	)	openbuy665	()	;
else if	(	BuyCnt==665	)	openbuy666	()	;
else if	(	BuyCnt==666	)	openbuy667	()	;
else if	(	BuyCnt==667	)	openbuy668	()	;
else if	(	BuyCnt==668	)	openbuy669	()	;
else if	(	BuyCnt==669	)	openbuy670	()	;
else if	(	BuyCnt==670	)	openbuy671	()	;
else if	(	BuyCnt==671	)	openbuy672	()	;
else if	(	BuyCnt==672	)	openbuy673	()	;
else if	(	BuyCnt==673	)	openbuy674	()	;
else if	(	BuyCnt==674	)	openbuy675	()	;
else if	(	BuyCnt==675	)	openbuy676	()	;
else if	(	BuyCnt==676	)	openbuy677	()	;
else if	(	BuyCnt==677	)	openbuy678	()	;
else if	(	BuyCnt==678	)	openbuy679	()	;
else if	(	BuyCnt==679	)	openbuy680	()	;
else if	(	BuyCnt==680	)	openbuy681	()	;
else if	(	BuyCnt==681	)	openbuy682	()	;
else if	(	BuyCnt==682	)	openbuy683	()	;
else if	(	BuyCnt==683	)	openbuy684	()	;
else if	(	BuyCnt==684	)	openbuy685	()	;
else if	(	BuyCnt==685	)	openbuy686	()	;
else if	(	BuyCnt==686	)	openbuy687	()	;
else if	(	BuyCnt==687	)	openbuy688	()	;
else if	(	BuyCnt==688	)	openbuy689	()	;
else if	(	BuyCnt==689	)	openbuy690	()	;
else if	(	BuyCnt==690	)	openbuy691	()	;
else if	(	BuyCnt==691	)	openbuy692	()	;
else if	(	BuyCnt==692	)	openbuy693	()	;
else if	(	BuyCnt==693	)	openbuy694	()	;
else if	(	BuyCnt==694	)	openbuy695	()	;
else if	(	BuyCnt==695	)	openbuy696	()	;
else if	(	BuyCnt==696	)	openbuy697	()	;
else if	(	BuyCnt==697	)	openbuy698	()	;
else if	(	BuyCnt==698	)	openbuy699	()	;
else if	(	BuyCnt==699	)	openbuy700	()	;
else if	(	BuyCnt==700	)	openbuy701	()	;
else if	(	BuyCnt==701	)	openbuy702	()	;
else if	(	BuyCnt==702	)	openbuy703	()	;
else if	(	BuyCnt==703	)	openbuy704	()	;
else if	(	BuyCnt==704	)	openbuy705	()	;
else if	(	BuyCnt==705	)	openbuy706	()	;
else if	(	BuyCnt==706	)	openbuy707	()	;
else if	(	BuyCnt==707	)	openbuy708	()	;
else if	(	BuyCnt==708	)	openbuy709	()	;
else if	(	BuyCnt==709	)	openbuy710	()	;
else if	(	BuyCnt==710	)	openbuy711	()	;
else if	(	BuyCnt==711	)	openbuy712	()	;
else if	(	BuyCnt==712	)	openbuy713	()	;
else if	(	BuyCnt==713	)	openbuy714	()	;
else if	(	BuyCnt==714	)	openbuy715	()	;
else if	(	BuyCnt==715	)	openbuy716	()	;
else if	(	BuyCnt==716	)	openbuy717	()	;
else if	(	BuyCnt==717	)	openbuy718	()	;
else if	(	BuyCnt==718	)	openbuy719	()	;
else if	(	BuyCnt==719	)	openbuy720	()	;
else if	(	BuyCnt==720	)	openbuy721	()	;
else if	(	BuyCnt==721	)	openbuy722	()	;
else if	(	BuyCnt==722	)	openbuy723	()	;
else if	(	BuyCnt==723	)	openbuy724	()	;
else if	(	BuyCnt==724	)	openbuy725	()	;
else if	(	BuyCnt==725	)	openbuy726	()	;
else if	(	BuyCnt==726	)	openbuy727	()	;
else if	(	BuyCnt==727	)	openbuy728	()	;
else if	(	BuyCnt==728	)	openbuy729	()	;
else if	(	BuyCnt==729	)	openbuy730	()	;
else if	(	BuyCnt==730	)	openbuy731	()	;
else if	(	BuyCnt==731	)	openbuy732	()	;
else if	(	BuyCnt==732	)	openbuy733	()	;
else if	(	BuyCnt==733	)	openbuy734	()	;
else if	(	BuyCnt==734	)	openbuy735	()	;
else if	(	BuyCnt==735	)	openbuy736	()	;
else if	(	BuyCnt==736	)	openbuy737	()	;
else if	(	BuyCnt==737	)	openbuy738	()	;
else if	(	BuyCnt==738	)	openbuy739	()	;
else if	(	BuyCnt==739	)	openbuy740	()	;
else if	(	BuyCnt==740	)	openbuy741	()	;
else if	(	BuyCnt==741	)	openbuy742	()	;
else if	(	BuyCnt==742	)	openbuy743	()	;
else if	(	BuyCnt==743	)	openbuy744	()	;
else if	(	BuyCnt==744	)	openbuy745	()	;
else if	(	BuyCnt==745	)	openbuy746	()	;
else if	(	BuyCnt==746	)	openbuy747	()	;
else if	(	BuyCnt==747	)	openbuy748	()	;
else if	(	BuyCnt==748	)	openbuy749	()	;
else if	(	BuyCnt==749	)	openbuy750	()	;
else if	(	BuyCnt==750	)	openbuy751	()	;
else if	(	BuyCnt==751	)	openbuy752	()	;
else if	(	BuyCnt==752	)	openbuy753	()	;
else if	(	BuyCnt==753	)	openbuy754	()	;
else if	(	BuyCnt==754	)	openbuy755	()	;
else if	(	BuyCnt==755	)	openbuy756	()	;
else if	(	BuyCnt==756	)	openbuy757	()	;
else if	(	BuyCnt==757	)	openbuy758	()	;
else if	(	BuyCnt==758	)	openbuy759	()	;
else if	(	BuyCnt==759	)	openbuy760	()	;
else if	(	BuyCnt==760	)	openbuy761	()	;
else if	(	BuyCnt==761	)	openbuy762	()	;
else if	(	BuyCnt==762	)	openbuy763	()	;
else if	(	BuyCnt==763	)	openbuy764	()	;
else if	(	BuyCnt==764	)	openbuy765	()	;
else if	(	BuyCnt==765	)	openbuy766	()	;
else if	(	BuyCnt==766	)	openbuy767	()	;
else if	(	BuyCnt==767	)	openbuy768	()	;
else if	(	BuyCnt==768	)	openbuy769	()	;
else if	(	BuyCnt==769	)	openbuy770	()	;
else if	(	BuyCnt==770	)	openbuy771	()	;
else if	(	BuyCnt==771	)	openbuy772	()	;
else if	(	BuyCnt==772	)	openbuy773	()	;
else if	(	BuyCnt==773	)	openbuy774	()	;
else if	(	BuyCnt==774	)	openbuy775	()	;
else if	(	BuyCnt==775	)	openbuy776	()	;
else if	(	BuyCnt==776	)	openbuy777	()	;
else if	(	BuyCnt==777	)	openbuy778	()	;
else if	(	BuyCnt==778	)	openbuy779	()	;
else if	(	BuyCnt==779	)	openbuy780	()	;
else if	(	BuyCnt==780	)	openbuy781	()	;
else if	(	BuyCnt==781	)	openbuy782	()	;
else if	(	BuyCnt==782	)	openbuy783	()	;
else if	(	BuyCnt==783	)	openbuy784	()	;
else if	(	BuyCnt==784	)	openbuy785	()	;
else if	(	BuyCnt==785	)	openbuy786	()	;
else if	(	BuyCnt==786	)	openbuy787	()	;
else if	(	BuyCnt==787	)	openbuy788	()	;
else if	(	BuyCnt==788	)	openbuy789	()	;
else if	(	BuyCnt==789	)	openbuy790	()	;
else if	(	BuyCnt==790	)	openbuy791	()	;
else if	(	BuyCnt==791	)	openbuy792	()	;
else if	(	BuyCnt==792	)	openbuy793	()	;
else if	(	BuyCnt==793	)	openbuy794	()	;
else if	(	BuyCnt==794	)	openbuy795	()	;
else if	(	BuyCnt==795	)	openbuy796	()	;
else if	(	BuyCnt==796	)	openbuy797	()	;
else if	(	BuyCnt==797	)	openbuy798	()	;
else if	(	BuyCnt==798	)	openbuy799	()	;
else if	(	BuyCnt==799	)	openbuy800	()	;
else if	(	BuyCnt==800	)	openbuy801	()	;
else if	(	BuyCnt==801	)	openbuy802	()	;
else if	(	BuyCnt==802	)	openbuy803	()	;
else if	(	BuyCnt==803	)	openbuy804	()	;
else if	(	BuyCnt==804	)	openbuy805	()	;
else if	(	BuyCnt==805	)	openbuy806	()	;
else if	(	BuyCnt==806	)	openbuy807	()	;
else if	(	BuyCnt==807	)	openbuy808	()	;
else if	(	BuyCnt==808	)	openbuy809	()	;
else if	(	BuyCnt==809	)	openbuy810	()	;
else if	(	BuyCnt==810	)	openbuy811	()	;
else if	(	BuyCnt==811	)	openbuy812	()	;
else if	(	BuyCnt==812	)	openbuy813	()	;
else if	(	BuyCnt==813	)	openbuy814	()	;
else if	(	BuyCnt==814	)	openbuy815	()	;
else if	(	BuyCnt==815	)	openbuy816	()	;
else if	(	BuyCnt==816	)	openbuy817	()	;
else if	(	BuyCnt==817	)	openbuy818	()	;
else if	(	BuyCnt==818	)	openbuy819	()	;
else if	(	BuyCnt==819	)	openbuy820	()	;
else if	(	BuyCnt==820	)	openbuy821	()	;
else if	(	BuyCnt==821	)	openbuy822	()	;
else if	(	BuyCnt==822	)	openbuy823	()	;
else if	(	BuyCnt==823	)	openbuy824	()	;
else if	(	BuyCnt==824	)	openbuy825	()	;
else if	(	BuyCnt==825	)	openbuy826	()	;
else if	(	BuyCnt==826	)	openbuy827	()	;
else if	(	BuyCnt==827	)	openbuy828	()	;
else if	(	BuyCnt==828	)	openbuy829	()	;
else if	(	BuyCnt==829	)	openbuy830	()	;
else if	(	BuyCnt==830	)	openbuy831	()	;
else if	(	BuyCnt==831	)	openbuy832	()	;
else if	(	BuyCnt==832	)	openbuy833	()	;
else if	(	BuyCnt==833	)	openbuy834	()	;
else if	(	BuyCnt==834	)	openbuy835	()	;
else if	(	BuyCnt==835	)	openbuy836	()	;
else if	(	BuyCnt==836	)	openbuy837	()	;
else if	(	BuyCnt==837	)	openbuy838	()	;
else if	(	BuyCnt==838	)	openbuy839	()	;
else if	(	BuyCnt==839	)	openbuy840	()	;
else if	(	BuyCnt==840	)	openbuy841	()	;
else if	(	BuyCnt==841	)	openbuy842	()	;
else if	(	BuyCnt==842	)	openbuy843	()	;
else if	(	BuyCnt==843	)	openbuy844	()	;
else if	(	BuyCnt==844	)	openbuy845	()	;
else if	(	BuyCnt==845	)	openbuy846	()	;
else if	(	BuyCnt==846	)	openbuy847	()	;
else if	(	BuyCnt==847	)	openbuy848	()	;
else if	(	BuyCnt==848	)	openbuy849	()	;
else if	(	BuyCnt==849	)	openbuy850	()	;
else if	(	BuyCnt==850	)	openbuy851	()	;
else if	(	BuyCnt==851	)	openbuy852	()	;
else if	(	BuyCnt==852	)	openbuy853	()	;
else if	(	BuyCnt==853	)	openbuy854	()	;
else if	(	BuyCnt==854	)	openbuy855	()	;
else if	(	BuyCnt==855	)	openbuy856	()	;
else if	(	BuyCnt==856	)	openbuy857	()	;
else if	(	BuyCnt==857	)	openbuy858	()	;
else if	(	BuyCnt==858	)	openbuy859	()	;
else if	(	BuyCnt==859	)	openbuy860	()	;
else if	(	BuyCnt==860	)	openbuy861	()	;
else if	(	BuyCnt==861	)	openbuy862	()	;
else if	(	BuyCnt==862	)	openbuy863	()	;
else if	(	BuyCnt==863	)	openbuy864	()	;
else if	(	BuyCnt==864	)	openbuy865	()	;
else if	(	BuyCnt==865	)	openbuy866	()	;
else if	(	BuyCnt==866	)	openbuy867	()	;
else if	(	BuyCnt==867	)	openbuy868	()	;
else if	(	BuyCnt==868	)	openbuy869	()	;
else if	(	BuyCnt==869	)	openbuy870	()	;
else if	(	BuyCnt==870	)	openbuy871	()	;
else if	(	BuyCnt==871	)	openbuy872	()	;
else if	(	BuyCnt==872	)	openbuy873	()	;
else if	(	BuyCnt==873	)	openbuy874	()	;
else if	(	BuyCnt==874	)	openbuy875	()	;
else if	(	BuyCnt==875	)	openbuy876	()	;
else if	(	BuyCnt==876	)	openbuy877	()	;
else if	(	BuyCnt==877	)	openbuy878	()	;
else if	(	BuyCnt==878	)	openbuy879	()	;
else if	(	BuyCnt==879	)	openbuy880	()	;
else if	(	BuyCnt==880	)	openbuy881	()	;
else if	(	BuyCnt==881	)	openbuy882	()	;
else if	(	BuyCnt==882	)	openbuy883	()	;
else if	(	BuyCnt==883	)	openbuy884	()	;
else if	(	BuyCnt==884	)	openbuy885	()	;
else if	(	BuyCnt==885	)	openbuy886	()	;
else if	(	BuyCnt==886	)	openbuy887	()	;
else if	(	BuyCnt==887	)	openbuy888	()	;
else if	(	BuyCnt==888	)	openbuy889	()	;
else if	(	BuyCnt==889	)	openbuy890	()	;
else if	(	BuyCnt==890	)	openbuy891	()	;
else if	(	BuyCnt==891	)	openbuy892	()	;
else if	(	BuyCnt==892	)	openbuy893	()	;
else if	(	BuyCnt==893	)	openbuy894	()	;
else if	(	BuyCnt==894	)	openbuy895	()	;
else if	(	BuyCnt==895	)	openbuy896	()	;
else if	(	BuyCnt==896	)	openbuy897	()	;
else if	(	BuyCnt==897	)	openbuy898	()	;
else if	(	BuyCnt==898	)	openbuy899	()	;
else if	(	BuyCnt==899	)	openbuy900	()	;
else if	(	BuyCnt==900	)	openbuy901	()	;
else if	(	BuyCnt==901	)	openbuy902	()	;
else if	(	BuyCnt==902	)	openbuy903	()	;
else if	(	BuyCnt==903	)	openbuy904	()	;
else if	(	BuyCnt==904	)	openbuy905	()	;
else if	(	BuyCnt==905	)	openbuy906	()	;
else if	(	BuyCnt==906	)	openbuy907	()	;
else if	(	BuyCnt==907	)	openbuy908	()	;
else if	(	BuyCnt==908	)	openbuy909	()	;
else if	(	BuyCnt==909	)	openbuy910	()	;
else if	(	BuyCnt==910	)	openbuy911	()	;
else if	(	BuyCnt==911	)	openbuy912	()	;
else if	(	BuyCnt==912	)	openbuy913	()	;
else if	(	BuyCnt==913	)	openbuy914	()	;
else if	(	BuyCnt==914	)	openbuy915	()	;
else if	(	BuyCnt==915	)	openbuy916	()	;
else if	(	BuyCnt==916	)	openbuy917	()	;
else if	(	BuyCnt==917	)	openbuy918	()	;
else if	(	BuyCnt==918	)	openbuy919	()	;
else if	(	BuyCnt==919	)	openbuy920	()	;
else if	(	BuyCnt==920	)	openbuy921	()	;
else if	(	BuyCnt==921	)	openbuy922	()	;
else if	(	BuyCnt==922	)	openbuy923	()	;
else if	(	BuyCnt==923	)	openbuy924	()	;
else if	(	BuyCnt==924	)	openbuy925	()	;
else if	(	BuyCnt==925	)	openbuy926	()	;
else if	(	BuyCnt==926	)	openbuy927	()	;
else if	(	BuyCnt==927	)	openbuy928	()	;
else if	(	BuyCnt==928	)	openbuy929	()	;
else if	(	BuyCnt==929	)	openbuy930	()	;
else if	(	BuyCnt==930	)	openbuy931	()	;
else if	(	BuyCnt==931	)	openbuy932	()	;
else if	(	BuyCnt==932	)	openbuy933	()	;
else if	(	BuyCnt==933	)	openbuy934	()	;
else if	(	BuyCnt==934	)	openbuy935	()	;
else if	(	BuyCnt==935	)	openbuy936	()	;
else if	(	BuyCnt==936	)	openbuy937	()	;
else if	(	BuyCnt==937	)	openbuy938	()	;
else if	(	BuyCnt==938	)	openbuy939	()	;
else if	(	BuyCnt==939	)	openbuy940	()	;
else if	(	BuyCnt==940	)	openbuy941	()	;
else if	(	BuyCnt==941	)	openbuy942	()	;
else if	(	BuyCnt==942	)	openbuy943	()	;
else if	(	BuyCnt==943	)	openbuy944	()	;
else if	(	BuyCnt==944	)	openbuy945	()	;
else if	(	BuyCnt==945	)	openbuy946	()	;
else if	(	BuyCnt==946	)	openbuy947	()	;
else if	(	BuyCnt==947	)	openbuy948	()	;
else if	(	BuyCnt==948	)	openbuy949	()	;
else if	(	BuyCnt==949	)	openbuy950	()	;
else if	(	BuyCnt==950	)	openbuy951	()	;
else if	(	BuyCnt==951	)	openbuy952	()	;
else if	(	BuyCnt==952	)	openbuy953	()	;
else if	(	BuyCnt==953	)	openbuy954	()	;
else if	(	BuyCnt==954	)	openbuy955	()	;
else if	(	BuyCnt==955	)	openbuy956	()	;
else if	(	BuyCnt==956	)	openbuy957	()	;
else if	(	BuyCnt==957	)	openbuy958	()	;
else if	(	BuyCnt==958	)	openbuy959	()	;
else if	(	BuyCnt==959	)	openbuy960	()	;
else if	(	BuyCnt==960	)	openbuy961	()	;
else if	(	BuyCnt==961	)	openbuy962	()	;
else if	(	BuyCnt==962	)	openbuy963	()	;
else if	(	BuyCnt==963	)	openbuy964	()	;
else if	(	BuyCnt==964	)	openbuy965	()	;
else if	(	BuyCnt==965	)	openbuy966	()	;
else if	(	BuyCnt==966	)	openbuy967	()	;
else if	(	BuyCnt==967	)	openbuy968	()	;
else if	(	BuyCnt==968	)	openbuy969	()	;
else if	(	BuyCnt==969	)	openbuy970	()	;
else if	(	BuyCnt==970	)	openbuy971	()	;
else if	(	BuyCnt==971	)	openbuy972	()	;
else if	(	BuyCnt==972	)	openbuy973	()	;
else if	(	BuyCnt==973	)	openbuy974	()	;
else if	(	BuyCnt==974	)	openbuy975	()	;
else if	(	BuyCnt==975	)	openbuy976	()	;
else if	(	BuyCnt==976	)	openbuy977	()	;
else if	(	BuyCnt==977	)	openbuy978	()	;
else if	(	BuyCnt==978	)	openbuy979	()	;
else if	(	BuyCnt==979	)	openbuy980	()	;
else if	(	BuyCnt==980	)	openbuy981	()	;
else if	(	BuyCnt==981	)	openbuy982	()	;
else if	(	BuyCnt==982	)	openbuy983	()	;
else if	(	BuyCnt==983	)	openbuy984	()	;
else if	(	BuyCnt==984	)	openbuy985	()	;
else if	(	BuyCnt==985	)	openbuy986	()	;
else if	(	BuyCnt==986	)	openbuy987	()	;
else if	(	BuyCnt==987	)	openbuy988	()	;
else if	(	BuyCnt==988	)	openbuy989	()	;
else if	(	BuyCnt==989	)	openbuy990	()	;
else if	(	BuyCnt==990	)	openbuy991	()	;
else if	(	BuyCnt==991	)	openbuy992	()	;
else if	(	BuyCnt==992	)	openbuy993	()	;
else if	(	BuyCnt==993	)	openbuy994	()	;
else if	(	BuyCnt==994	)	openbuy995	()	;
else if	(	BuyCnt==995	)	openbuy996	()	;
else if	(	BuyCnt==996	)	openbuy997	()	;
else if	(	BuyCnt==997	)	openbuy998	()	;
else if	(	BuyCnt==998	)	openbuy999	()	;
else if	(	BuyCnt==999	)	openbuy1000	()	;

    
  }
  
//+------------------------------------------------------------------+
void openbuy1 ()
{
double lot = 0.01;
  double StockasticCurrent = iStochastic(NULL,0,5,3,3,MODE_SMA,0,MODE_MAIN,1);
  double StockasticPrevious = iStochastic(NULL,0,5,3,3,MODE_SMA,0,MODE_MAIN,2);
  
 
  if (StockasticPrevious < lowlevel)
  if (StockasticCurrent>lowlevel) 

if (Close[0]<maxprice)
if (Close[0]>lowprice)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);

buy1 = Close[0]-Close[0]/100*steplevel1;
GlobalVariableSet(test1,buy1);
}
void openbuy2 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy1)
if (Close[0]>lowprice)

if(b>d)

buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green); 
buy2 = buy1 - Close[0]/100*steplevel1;
GlobalVariableSet(test2,buy2);
}
void openbuy3 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy2)
if (Close[0]>lowprice)

if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy3 = buy2 - Close[0]/100*steplevel1;
GlobalVariableSet(test3,buy3);
}
void openbuy4 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy3)
if (Close[0]>lowprice)

if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green); 
buy4 = buy3 - Close[0]/100*steplevel1;
GlobalVariableSet(test4,buy4);
}
void openbuy5 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy4)
if (Close[0]>lowprice)

if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy5 = buy4 - Close[0]/100*steplevel1;
GlobalVariableSet(test5,buy5);
}
void openbuy6 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy5)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green); 
buy6 = buy5 - Close[0]/100*steplevel1;
GlobalVariableSet(test6,buy6);
}
void openbuy7 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy6)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy7 = buy6 - Close[0]/100*steplevel1;
GlobalVariableSet(test7,buy7);
}
void openbuy8 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy7)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green); 
buy8 = buy7 - Close[0]/100*steplevel1;
GlobalVariableSet(test8,buy8);
}
void openbuy9 ()
{

double b = Close[0];
double lot =0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy8)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy9 = buy8 - Close[0]/100*steplevel1;
GlobalVariableSet(test9,buy9);
}
void openbuy10 ()
{

double b = Close[0];
double lot =0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy9)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy10 = buy9 - Close[0]/100*steplevel1;
GlobalVariableSet(test10,buy10);
}
void openbuy11 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy10)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy11 = buy10 - Close[0]/100*steplevel1;
GlobalVariableSet(test11,buy11);
}
void openbuy12 ()
{

double b = Close[0];
double lot =0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy11)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green); 
buy12 = buy11 - Close[0]/100*steplevel1;
GlobalVariableSet(test12,buy12);
}
void openbuy13 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy12)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy13 = buy12 - Close[0]/100*steplevel1;
GlobalVariableSet(test13,buy13);
}
void openbuy14 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy13)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy14 = buy13 - Close[0]/100*steplevel1;
GlobalVariableSet(test14,buy14);
}
void openbuy15 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy14)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy15 = buy14 - Close[0]/100*steplevel1;
GlobalVariableSet(test15,buy15);
}
void openbuy16 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy15)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy16 = buy15 - Close[0]/100*steplevel1;
GlobalVariableSet(test16,buy16);
}
void openbuy17 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy16)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy17 = buy16 - Close[0]/100*steplevel1;
GlobalVariableSet(test17,buy17);
}
void openbuy18 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy17)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy18 = buy17 - Close[0]/100*steplevel1;
GlobalVariableSet(test18,buy18);
}
void openbuy19 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy18)
if (Close[0]>lowprice)
if(b>d)

buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy19 = buy18 - Close[0]/100*steplevel1;
GlobalVariableSet(test19,buy19);
}
void openbuy20 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy19)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy20 = buy19 - Close[0]/100*steplevel1;
GlobalVariableSet(test20,buy20);
}
void openbuy21 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy20)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy21 = buy20 - Close[0]/100*steplevel1;
GlobalVariableSet(test21,buy21);
}
void openbuy22 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy21)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy22 = buy21 - Close[0]/100*steplevel1;
GlobalVariableSet(test22,buy22);
}
void openbuy23 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy22)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy23 = buy22 - Close[0]/100*steplevel1;
GlobalVariableSet(test23,buy23);
}
void openbuy24 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy23)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy24 = buy23 - Close[0]/100*steplevel1;
GlobalVariableSet(test24,buy24);
}
void openbuy25 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy24)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy25 = buy24 - Close[0]/100*steplevel1;
GlobalVariableSet(test25,buy25);
}
void openbuy26 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy25)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green); 
buy26 = buy25 - Close[0]/100*steplevel1;
GlobalVariableSet(test26,buy26);
}
void openbuy27 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy26)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy27 = buy26 - Close[0]/100*steplevel1;
GlobalVariableSet(test27,buy27);
}
void openbuy28 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy27)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy28 = buy27 - Close[0]/100*steplevel1;
GlobalVariableSet(test28,buy28);
}
void openbuy29 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy28)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green); 
buy29 = buy28 - Close[0]/100*steplevel1;
GlobalVariableSet(test29,buy29);
}
void openbuy30 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy29)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy30 = buy29 - Close[0]/100*steplevel1;
GlobalVariableSet(test30,buy30);
}
void openbuy31 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy30)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy31 = buy30 - Close[0]/100*steplevel1;
GlobalVariableSet(test31,buy31);
}
void openbuy32 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy31)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy32 = buy31 - Close[0]/100*steplevel1;
GlobalVariableSet(test32,buy32);
}
void openbuy33 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy32)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy33 = buy32 - Close[0]/100*steplevel1;
GlobalVariableSet(test33,buy33);
}
void openbuy34 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy33)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy34 = buy33 - Close[0]/100*steplevel1;
GlobalVariableSet(test34,buy34);
}
void openbuy35 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy34)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy35 = buy34 - Close[0]/100*steplevel1;
GlobalVariableSet(test35,buy35);
}
void openbuy36 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy35)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy36 = buy35 - Close[0]/100*steplevel1;
GlobalVariableSet(test36,buy36);
}
void openbuy37 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy36)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy37 = buy36 - Close[0]/100*steplevel1;
GlobalVariableSet(test37,buy37);
}
void openbuy38 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy37)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy38 = buy37 - Close[0]/100*steplevel1;
GlobalVariableSet(test38,buy38);
}
void openbuy39 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy38)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy39 = buy38 - Close[0]/100*steplevel1;
GlobalVariableSet(test39,buy39);
}
void openbuy40 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy39)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy40 = buy39 - Close[0]/100*steplevel1;
GlobalVariableSet(test40,buy40);
}
void openbuy41 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy40)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy41 = buy40 - Close[0]/100*steplevel1;
GlobalVariableSet(test41,buy41);
}
void openbuy42 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy41)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy42 = buy41 - Close[0]/100*steplevel1;
GlobalVariableSet(test42,buy42);
}
void openbuy43 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy42)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy43 = buy42 - Close[0]/100*steplevel1;
GlobalVariableSet(test43,buy43);
}
void openbuy44 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy43)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy44 = buy43 - Close[0]/100*steplevel1;
GlobalVariableSet(test44,buy44);
}
void openbuy45 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy44)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy45 = buy44 - Close[0]/100*steplevel1;
GlobalVariableSet(test45,buy45);
}
void openbuy46 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy45)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy46 = buy45 - Close[0]/100*steplevel1;
GlobalVariableSet(test46,buy46);
}
void openbuy47 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy46)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy47 = buy46 - Close[0]/100*steplevel1;
GlobalVariableSet(test47,buy47);
}
void openbuy48 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy47)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy48 = buy47 - Close[0]/100*steplevel1;
GlobalVariableSet(test48,buy48);
}
void openbuy49 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy48)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy49 = buy48 - Close[0]/100*steplevel1;
GlobalVariableSet(test49,buy49);
}
void openbuy50 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy49)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy50 = buy49 - Close[0]/100*steplevel1;
GlobalVariableSet(test50,buy50);
}
void openbuy51 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy50)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy51 = buy50 - Close[0]/100*steplevel1;
GlobalVariableSet(test51,buy51);
}
void openbuy52 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy51)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy52 = buy51 - Close[0]/100*steplevel1;
GlobalVariableSet(test52,buy52);
}
void openbuy53 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy52)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy53 = buy52 - Close[0]/100*steplevel1;
GlobalVariableSet(test53,buy53);
}
void openbuy54 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy53)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy54 = buy53 - Close[0]/100*steplevel1;
GlobalVariableSet(test54,buy54);
}
void openbuy55 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy54)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy55 = buy54 - Close[0]/100*steplevel1;
GlobalVariableSet(test55,buy55);
}
void openbuy56 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy55)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy56 = buy55 - Close[0]/100*steplevel1;
GlobalVariableSet(test56,buy56);
}
void openbuy57 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy56)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy57 = buy56 - Close[0]/100*steplevel1;
GlobalVariableSet(test57,buy57);
}
void openbuy58 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy57)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy58 = buy57 - Close[0]/100*steplevel1;
GlobalVariableSet(test58,buy58);
}
void openbuy59 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy58)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy59 = buy58 - Close[0]/100*steplevel1;
GlobalVariableSet(test59,buy59);
}
void openbuy60 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy59)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy60 = buy59 - Close[0]/100*steplevel1;
GlobalVariableSet(test60,buy60);
}
void openbuy61 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy60)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy61 = buy60 - Close[0]/100*steplevel1;
GlobalVariableSet(test61,buy61);
}
void openbuy62 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy61)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy62 = buy61 - Close[0]/100*steplevel1;
GlobalVariableSet(test62,buy62);
}
void openbuy63 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy62)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy63 = buy62 - Close[0]/100*steplevel1;
GlobalVariableSet(test63,buy63);
}
void openbuy64 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy63)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy64 = buy63 - Close[0]/100*steplevel1;
GlobalVariableSet(test64,buy64);
}
void openbuy65 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy64)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy65 = buy64 - Close[0]/100*steplevel1;
GlobalVariableSet(test65,buy65);
}
void openbuy66 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy65)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy66 = buy65 - Close[0]/100*steplevel1;
GlobalVariableSet(test66,buy66);
}
void openbuy67 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy66)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy67 = buy66 - Close[0]/100*steplevel1;
GlobalVariableSet(test67,buy67);
}
void openbuy68 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy67)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy68 = buy67 - Close[0]/100*steplevel1;
GlobalVariableSet(test68,buy68);
}
void openbuy69 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy68)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy69 = buy68 - Close[0]/100*steplevel1;
GlobalVariableSet(test69,buy69);
}
void openbuy70 ()
{

double b = Close[0];
double lot = 0.01;
double c = Close[1]; 
double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);
if(b<buy69)
if (Close[0]>lowprice)
if(b>d)
buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);  
buy70 = buy69 - Close[0]/100*steplevel1;
GlobalVariableSet(test70,buy70);
}

void openbuy71 (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);if(b<buy69)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green); buy71 = buy70 - Close[0]/100*steplevel1;GlobalVariableSet(test71,buy71);}


void	openbuy72	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy70	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy72	=	buy71	    - Close[0]/100*steplevel1;GlobalVariableSet(			test72	,	buy72	);}
void	openbuy73	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy71	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy73	=	buy72	    - Close[0]/100*steplevel1;GlobalVariableSet(			test73	,	buy73	);}
void	openbuy74	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy72	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy74	=	buy73	    - Close[0]/100*steplevel1;GlobalVariableSet(			test74	,	buy74	);}
void	openbuy75	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy73	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy75	=	buy74	    - Close[0]/100*steplevel1;GlobalVariableSet(			test75	,	buy75	);}
void	openbuy76	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy74	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy76	=	buy75	    - Close[0]/100*steplevel1;GlobalVariableSet(			test76	,	buy76	);}
void	openbuy77	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy75	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy77	=	buy76	    - Close[0]/100*steplevel1;GlobalVariableSet(			test77	,	buy77	);}
void	openbuy78	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy76	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy78	=	buy77	    - Close[0]/100*steplevel1;GlobalVariableSet(			test78	,	buy78	);}
void	openbuy79	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy77	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy79	=	buy78	    - Close[0]/100*steplevel1;GlobalVariableSet(			test79	,	buy79	);}
void	openbuy80	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy78	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy80	=	buy79	    - Close[0]/100*steplevel1;GlobalVariableSet(			test80	,	buy80	);}
void	openbuy81	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy79	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy81	=	buy80	    - Close[0]/100*steplevel1;GlobalVariableSet(			test81	,	buy81	);}
void	openbuy82	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy80	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy82	=	buy81	    - Close[0]/100*steplevel1;GlobalVariableSet(			test82	,	buy82	);}
void	openbuy83	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy81	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy83	=	buy82	    - Close[0]/100*steplevel1;GlobalVariableSet(			test83	,	buy83	);}
void	openbuy84	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy82	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy84	=	buy83	    - Close[0]/100*steplevel1;GlobalVariableSet(			test84	,	buy84	);}
void	openbuy85	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy83	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy85	=	buy84	    - Close[0]/100*steplevel1;GlobalVariableSet(			test85	,	buy85	);}
void	openbuy86	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy84	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy86	=	buy85	    - Close[0]/100*steplevel1;GlobalVariableSet(			test86	,	buy86	);}
void	openbuy87	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy85	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy87	=	buy86	    - Close[0]/100*steplevel1;GlobalVariableSet(			test87	,	buy87	);}
void	openbuy88	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy86	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy88	=	buy87	    - Close[0]/100*steplevel1;GlobalVariableSet(			test88	,	buy88	);}
void	openbuy89	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy87	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy89	=	buy88	    - Close[0]/100*steplevel1;GlobalVariableSet(			test89	,	buy89	);}
void	openbuy90	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy88	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy90	=	buy89	    - Close[0]/100*steplevel1;GlobalVariableSet(			test90	,	buy90	);}
void	openbuy91	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy89	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy91	=	buy90	    - Close[0]/100*steplevel1;GlobalVariableSet(			test91	,	buy91	);}
void	openbuy92	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy90	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy92	=	buy91	    - Close[0]/100*steplevel1;GlobalVariableSet(			test92	,	buy92	);}
void	openbuy93	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy91	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy93	=	buy92	    - Close[0]/100*steplevel1;GlobalVariableSet(			test93	,	buy93	);}
void	openbuy94	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy92	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy94	=	buy93	    - Close[0]/100*steplevel1;GlobalVariableSet(			test94	,	buy94	);}
void	openbuy95	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy93	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy95	=	buy94	    - Close[0]/100*steplevel1;GlobalVariableSet(			test95	,	buy95	);}
void	openbuy96	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy94	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy96	=	buy95	    - Close[0]/100*steplevel1;GlobalVariableSet(			test96	,	buy96	);}
void	openbuy97	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy95	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy97	=	buy96	    - Close[0]/100*steplevel1;GlobalVariableSet(			test97	,	buy97	);}
void	openbuy98	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy96	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy98	=	buy97	    - Close[0]/100*steplevel1;GlobalVariableSet(			test98	,	buy98	);}
void	openbuy99	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy97	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy99	=	buy98	    - Close[0]/100*steplevel1;GlobalVariableSet(			test99	,	buy99	);}
void	openbuy100	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy98	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy100	=	buy99	    - Close[0]/100*steplevel1;GlobalVariableSet(			test100	,	buy100	);}
void	openbuy101	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy99	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy101	=	buy100	    - Close[0]/100*steplevel1;GlobalVariableSet(			test101	,	buy101	);}
void	openbuy102	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy100	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy102	=	buy101	    - Close[0]/100*steplevel1;GlobalVariableSet(			test102	,	buy102	);}
void	openbuy103	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy101	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy103	=	buy102	    - Close[0]/100*steplevel1;GlobalVariableSet(			test103	,	buy103	);}
void	openbuy104	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy102	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy104	=	buy103	    - Close[0]/100*steplevel1;GlobalVariableSet(			test104	,	buy104	);}
void	openbuy105	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy103	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy105	=	buy104	    - Close[0]/100*steplevel1;GlobalVariableSet(			test105	,	buy105	);}
void	openbuy106	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy104	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy106	=	buy105	    - Close[0]/100*steplevel1;GlobalVariableSet(			test106	,	buy106	);}
void	openbuy107	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy105	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy107	=	buy106	    - Close[0]/100*steplevel1;GlobalVariableSet(			test107	,	buy107	);}
void	openbuy108	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy106	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy108	=	buy107	    - Close[0]/100*steplevel1;GlobalVariableSet(			test108	,	buy108	);}
void	openbuy109	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy107	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy109	=	buy108	    - Close[0]/100*steplevel1;GlobalVariableSet(			test109	,	buy109	);}
void	openbuy110	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy108	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy110	=	buy109	    - Close[0]/100*steplevel1;GlobalVariableSet(			test110	,	buy110	);}
void	openbuy111	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy109	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy111	=	buy110	    - Close[0]/100*steplevel1;GlobalVariableSet(			test111	,	buy111	);}
void	openbuy112	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy110	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy112	=	buy111	    - Close[0]/100*steplevel1;GlobalVariableSet(			test112	,	buy112	);}
void	openbuy113	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy111	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy113	=	buy112	    - Close[0]/100*steplevel1;GlobalVariableSet(			test113	,	buy113	);}
void	openbuy114	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy112	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy114	=	buy113	    - Close[0]/100*steplevel1;GlobalVariableSet(			test114	,	buy114	);}
void	openbuy115	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy113	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy115	=	buy114	    - Close[0]/100*steplevel1;GlobalVariableSet(			test115	,	buy115	);}
void	openbuy116	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy114	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy116	=	buy115	    - Close[0]/100*steplevel1;GlobalVariableSet(			test116	,	buy116	);}
void	openbuy117	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy115	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy117	=	buy116	    - Close[0]/100*steplevel1;GlobalVariableSet(			test117	,	buy117	);}
void	openbuy118	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy116	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy118	=	buy117	    - Close[0]/100*steplevel1;GlobalVariableSet(			test118	,	buy118	);}
void	openbuy119	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy117	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy119	=	buy118	    - Close[0]/100*steplevel1;GlobalVariableSet(			test119	,	buy119	);}
void	openbuy120	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy118	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy120	=	buy119	    - Close[0]/100*steplevel1;GlobalVariableSet(			test120	,	buy120	);}
void	openbuy121	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy119	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy121	=	buy120	    - Close[0]/100*steplevel1;GlobalVariableSet(			test121	,	buy121	);}
void	openbuy122	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy120	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy122	=	buy121	    - Close[0]/100*steplevel1;GlobalVariableSet(			test122	,	buy122	);}
void	openbuy123	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy121	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy123	=	buy122	    - Close[0]/100*steplevel1;GlobalVariableSet(			test123	,	buy123	);}
void	openbuy124	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy122	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy124	=	buy123	    - Close[0]/100*steplevel1;GlobalVariableSet(			test124	,	buy124	);}
void	openbuy125	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy123	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy125	=	buy124	    - Close[0]/100*steplevel1;GlobalVariableSet(			test125	,	buy125	);}
void	openbuy126	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy124	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy126	=	buy125	    - Close[0]/100*steplevel1;GlobalVariableSet(			test126	,	buy126	);}
void	openbuy127	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy125	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy127	=	buy126	    - Close[0]/100*steplevel1;GlobalVariableSet(			test127	,	buy127	);}
void	openbuy128	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy126	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy128	=	buy127	    - Close[0]/100*steplevel1;GlobalVariableSet(			test128	,	buy128	);}
void	openbuy129	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy127	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy129	=	buy128	    - Close[0]/100*steplevel1;GlobalVariableSet(			test129	,	buy129	);}
void	openbuy130	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy128	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy130	=	buy129	    - Close[0]/100*steplevel1;GlobalVariableSet(			test130	,	buy130	);}
void	openbuy131	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy129	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy131	=	buy130	    - Close[0]/100*steplevel1;GlobalVariableSet(			test131	,	buy131	);}
void	openbuy132	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy130	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy132	=	buy131	    - Close[0]/100*steplevel1;GlobalVariableSet(			test132	,	buy132	);}
void	openbuy133	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy131	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy133	=	buy132	    - Close[0]/100*steplevel1;GlobalVariableSet(			test133	,	buy133	);}
void	openbuy134	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy132	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy134	=	buy133	    - Close[0]/100*steplevel1;GlobalVariableSet(			test134	,	buy134	);}
void	openbuy135	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy133	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy135	=	buy134	    - Close[0]/100*steplevel1;GlobalVariableSet(			test135	,	buy135	);}
void	openbuy136	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy134	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy136	=	buy135	    - Close[0]/100*steplevel1;GlobalVariableSet(			test136	,	buy136	);}
void	openbuy137	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy135	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy137	=	buy136	    - Close[0]/100*steplevel1;GlobalVariableSet(			test137	,	buy137	);}
void	openbuy138	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy136	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy138	=	buy137	    - Close[0]/100*steplevel1;GlobalVariableSet(			test138	,	buy138	);}
void	openbuy139	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy137	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy139	=	buy138	    - Close[0]/100*steplevel1;GlobalVariableSet(			test139	,	buy139	);}
void	openbuy140	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy138	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy140	=	buy139	    - Close[0]/100*steplevel1;GlobalVariableSet(			test140	,	buy140	);}
void	openbuy141	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy139	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy141	=	buy140	    - Close[0]/100*steplevel1;GlobalVariableSet(			test141	,	buy141	);}
void	openbuy142	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy140	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy142	=	buy141	    - Close[0]/100*steplevel1;GlobalVariableSet(			test142	,	buy142	);}
void	openbuy143	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy141	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy143	=	buy142	    - Close[0]/100*steplevel1;GlobalVariableSet(			test143	,	buy143	);}
void	openbuy144	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy142	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy144	=	buy143	    - Close[0]/100*steplevel1;GlobalVariableSet(			test144	,	buy144	);}
void	openbuy145	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy143	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy145	=	buy144	    - Close[0]/100*steplevel1;GlobalVariableSet(			test145	,	buy145	);}
void	openbuy146	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy144	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy146	=	buy145	    - Close[0]/100*steplevel1;GlobalVariableSet(			test146	,	buy146	);}
void	openbuy147	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy145	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy147	=	buy146	    - Close[0]/100*steplevel1;GlobalVariableSet(			test147	,	buy147	);}
void	openbuy148	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy146	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy148	=	buy147	    - Close[0]/100*steplevel1;GlobalVariableSet(			test148	,	buy148	);}
void	openbuy149	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy147	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy149	=	buy148	    - Close[0]/100*steplevel1;GlobalVariableSet(			test149	,	buy149	);}
void	openbuy150	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy148	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy150	=	buy149	    - Close[0]/100*steplevel1;GlobalVariableSet(			test150	,	buy150	);}
void	openbuy151	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy149	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy151	=	buy150	    - Close[0]/100*steplevel1;GlobalVariableSet(			test151	,	buy151	);}
void	openbuy152	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy150	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy152	=	buy151	    - Close[0]/100*steplevel1;GlobalVariableSet(			test152	,	buy152	);}
void	openbuy153	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy151	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy153	=	buy152	    - Close[0]/100*steplevel1;GlobalVariableSet(			test153	,	buy153	);}
void	openbuy154	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy152	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy154	=	buy153	    - Close[0]/100*steplevel1;GlobalVariableSet(			test154	,	buy154	);}
void	openbuy155	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy153	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy155	=	buy154	    - Close[0]/100*steplevel1;GlobalVariableSet(			test155	,	buy155	);}
void	openbuy156	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy154	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy156	=	buy155	    - Close[0]/100*steplevel1;GlobalVariableSet(			test156	,	buy156	);}
void	openbuy157	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy155	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy157	=	buy156	    - Close[0]/100*steplevel1;GlobalVariableSet(			test157	,	buy157	);}
void	openbuy158	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy156	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy158	=	buy157	    - Close[0]/100*steplevel1;GlobalVariableSet(			test158	,	buy158	);}
void	openbuy159	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy157	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy159	=	buy158	    - Close[0]/100*steplevel1;GlobalVariableSet(			test159	,	buy159	);}
void	openbuy160	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy158	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy160	=	buy159	    - Close[0]/100*steplevel1;GlobalVariableSet(			test160	,	buy160	);}
void	openbuy161	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy159	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy161	=	buy160	    - Close[0]/100*steplevel1;GlobalVariableSet(			test161	,	buy161	);}
void	openbuy162	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy160	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy162	=	buy161	    - Close[0]/100*steplevel1;GlobalVariableSet(			test162	,	buy162	);}
void	openbuy163	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy161	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy163	=	buy162	    - Close[0]/100*steplevel1;GlobalVariableSet(			test163	,	buy163	);}
void	openbuy164	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy162	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy164	=	buy163	    - Close[0]/100*steplevel1;GlobalVariableSet(			test164	,	buy164	);}
void	openbuy165	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy163	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy165	=	buy164	    - Close[0]/100*steplevel1;GlobalVariableSet(			test165	,	buy165	);}
void	openbuy166	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy164	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy166	=	buy165	    - Close[0]/100*steplevel1;GlobalVariableSet(			test166	,	buy166	);}
void	openbuy167	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy165	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy167	=	buy166	    - Close[0]/100*steplevel1;GlobalVariableSet(			test167	,	buy167	);}
void	openbuy168	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy166	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy168	=	buy167	    - Close[0]/100*steplevel1;GlobalVariableSet(			test168	,	buy168	);}
void	openbuy169	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy167	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy169	=	buy168	    - Close[0]/100*steplevel1;GlobalVariableSet(			test169	,	buy169	);}
void	openbuy170	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy168	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy170	=	buy169	    - Close[0]/100*steplevel1;GlobalVariableSet(			test170	,	buy170	);}
void	openbuy171	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy169	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy171	=	buy170	    - Close[0]/100*steplevel1;GlobalVariableSet(			test171	,	buy171	);}
void	openbuy172	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy170	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy172	=	buy171	    - Close[0]/100*steplevel1;GlobalVariableSet(			test172	,	buy172	);}
void	openbuy173	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy171	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy173	=	buy172	    - Close[0]/100*steplevel1;GlobalVariableSet(			test173	,	buy173	);}
void	openbuy174	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy172	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy174	=	buy173	    - Close[0]/100*steplevel1;GlobalVariableSet(			test174	,	buy174	);}
void	openbuy175	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy173	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy175	=	buy174	    - Close[0]/100*steplevel1;GlobalVariableSet(			test175	,	buy175	);}
void	openbuy176	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy174	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy176	=	buy175	    - Close[0]/100*steplevel1;GlobalVariableSet(			test176	,	buy176	);}
void	openbuy177	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy175	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy177	=	buy176	    - Close[0]/100*steplevel1;GlobalVariableSet(			test177	,	buy177	);}
void	openbuy178	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy176	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy178	=	buy177	    - Close[0]/100*steplevel1;GlobalVariableSet(			test178	,	buy178	);}
void	openbuy179	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy177	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy179	=	buy178	    - Close[0]/100*steplevel1;GlobalVariableSet(			test179	,	buy179	);}
void	openbuy180	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy178	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy180	=	buy179	    - Close[0]/100*steplevel1;GlobalVariableSet(			test180	,	buy180	);}
void	openbuy181	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy179	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy181	=	buy180	    - Close[0]/100*steplevel1;GlobalVariableSet(			test181	,	buy181	);}
void	openbuy182	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy180	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy182	=	buy181	    - Close[0]/100*steplevel1;GlobalVariableSet(			test182	,	buy182	);}
void	openbuy183	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy181	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy183	=	buy182	    - Close[0]/100*steplevel1;GlobalVariableSet(			test183	,	buy183	);}
void	openbuy184	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy182	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy184	=	buy183	    - Close[0]/100*steplevel1;GlobalVariableSet(			test184	,	buy184	);}
void	openbuy185	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy183	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy185	=	buy184	    - Close[0]/100*steplevel1;GlobalVariableSet(			test185	,	buy185	);}
void	openbuy186	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy184	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy186	=	buy185	    - Close[0]/100*steplevel1;GlobalVariableSet(			test186	,	buy186	);}
void	openbuy187	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy185	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy187	=	buy186	    - Close[0]/100*steplevel1;GlobalVariableSet(			test187	,	buy187	);}
void	openbuy188	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy186	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy188	=	buy187	    - Close[0]/100*steplevel1;GlobalVariableSet(			test188	,	buy188	);}
void	openbuy189	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy187	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy189	=	buy188	    - Close[0]/100*steplevel1;GlobalVariableSet(			test189	,	buy189	);}
void	openbuy190	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy188	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy190	=	buy189	    - Close[0]/100*steplevel1;GlobalVariableSet(			test190	,	buy190	);}
void	openbuy191	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy189	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy191	=	buy190	    - Close[0]/100*steplevel1;GlobalVariableSet(			test191	,	buy191	);}
void	openbuy192	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy190	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy192	=	buy191	    - Close[0]/100*steplevel1;GlobalVariableSet(			test192	,	buy192	);}
void	openbuy193	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy191	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy193	=	buy192	    - Close[0]/100*steplevel1;GlobalVariableSet(			test193	,	buy193	);}
void	openbuy194	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy192	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy194	=	buy193	    - Close[0]/100*steplevel1;GlobalVariableSet(			test194	,	buy194	);}
void	openbuy195	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy193	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy195	=	buy194	    - Close[0]/100*steplevel1;GlobalVariableSet(			test195	,	buy195	);}
void	openbuy196	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy194	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy196	=	buy195	    - Close[0]/100*steplevel1;GlobalVariableSet(			test196	,	buy196	);}
void	openbuy197	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy195	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy197	=	buy196	    - Close[0]/100*steplevel1;GlobalVariableSet(			test197	,	buy197	);}
void	openbuy198	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy196	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy198	=	buy197	    - Close[0]/100*steplevel1;GlobalVariableSet(			test198	,	buy198	);}
void	openbuy199	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy197	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy199	=	buy198	    - Close[0]/100*steplevel1;GlobalVariableSet(			test199	,	buy199	);}
void	openbuy200	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy198	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy200	=	buy199	    - Close[0]/100*steplevel1;GlobalVariableSet(			test200	,	buy200	);}
void	openbuy201	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy199	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy201	=	buy200	    - Close[0]/100*steplevel1;GlobalVariableSet(			test201	,	buy201	);}
void	openbuy202	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy200	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy202	=	buy201	    - Close[0]/100*steplevel1;GlobalVariableSet(			test202	,	buy202	);}
void	openbuy203	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy201	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy203	=	buy202	    - Close[0]/100*steplevel1;GlobalVariableSet(			test203	,	buy203	);}
void	openbuy204	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy202	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy204	=	buy203	    - Close[0]/100*steplevel1;GlobalVariableSet(			test204	,	buy204	);}
void	openbuy205	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy203	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy205	=	buy204	    - Close[0]/100*steplevel1;GlobalVariableSet(			test205	,	buy205	);}
void	openbuy206	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy204	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy206	=	buy205	    - Close[0]/100*steplevel1;GlobalVariableSet(			test206	,	buy206	);}
void	openbuy207	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy205	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy207	=	buy206	    - Close[0]/100*steplevel1;GlobalVariableSet(			test207	,	buy207	);}
void	openbuy208	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy206	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy208	=	buy207	    - Close[0]/100*steplevel1;GlobalVariableSet(			test208	,	buy208	);}
void	openbuy209	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy207	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy209	=	buy208	    - Close[0]/100*steplevel1;GlobalVariableSet(			test209	,	buy209	);}
void	openbuy210	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy208	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy210	=	buy209	    - Close[0]/100*steplevel1;GlobalVariableSet(			test210	,	buy210	);}
void	openbuy211	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy209	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy211	=	buy210	    - Close[0]/100*steplevel1;GlobalVariableSet(			test211	,	buy211	);}
void	openbuy212	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy210	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy212	=	buy211	    - Close[0]/100*steplevel1;GlobalVariableSet(			test212	,	buy212	);}
void	openbuy213	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy211	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy213	=	buy212	    - Close[0]/100*steplevel1;GlobalVariableSet(			test213	,	buy213	);}
void	openbuy214	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy212	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy214	=	buy213	    - Close[0]/100*steplevel1;GlobalVariableSet(			test214	,	buy214	);}
void	openbuy215	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy213	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy215	=	buy214	    - Close[0]/100*steplevel1;GlobalVariableSet(			test215	,	buy215	);}
void	openbuy216	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy214	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy216	=	buy215	    - Close[0]/100*steplevel1;GlobalVariableSet(			test216	,	buy216	);}
void	openbuy217	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy215	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy217	=	buy216	    - Close[0]/100*steplevel1;GlobalVariableSet(			test217	,	buy217	);}
void	openbuy218	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy216	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy218	=	buy217	    - Close[0]/100*steplevel1;GlobalVariableSet(			test218	,	buy218	);}
void	openbuy219	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy217	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy219	=	buy218	    - Close[0]/100*steplevel1;GlobalVariableSet(			test219	,	buy219	);}
void	openbuy220	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy218	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy220	=	buy219	    - Close[0]/100*steplevel1;GlobalVariableSet(			test220	,	buy220	);}
void	openbuy221	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy219	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy221	=	buy220	    - Close[0]/100*steplevel1;GlobalVariableSet(			test221	,	buy221	);}
void	openbuy222	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy220	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy222	=	buy221	    - Close[0]/100*steplevel1;GlobalVariableSet(			test222	,	buy222	);}
void	openbuy223	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy221	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy223	=	buy222	    - Close[0]/100*steplevel1;GlobalVariableSet(			test223	,	buy223	);}
void	openbuy224	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy222	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy224	=	buy223	    - Close[0]/100*steplevel1;GlobalVariableSet(			test224	,	buy224	);}
void	openbuy225	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy223	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy225	=	buy224	    - Close[0]/100*steplevel1;GlobalVariableSet(			test225	,	buy225	);}
void	openbuy226	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy224	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy226	=	buy225	    - Close[0]/100*steplevel1;GlobalVariableSet(			test226	,	buy226	);}
void	openbuy227	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy225	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy227	=	buy226	    - Close[0]/100*steplevel1;GlobalVariableSet(			test227	,	buy227	);}
void	openbuy228	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy226	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy228	=	buy227	    - Close[0]/100*steplevel1;GlobalVariableSet(			test228	,	buy228	);}
void	openbuy229	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy227	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy229	=	buy228	    - Close[0]/100*steplevel1;GlobalVariableSet(			test229	,	buy229	);}
void	openbuy230	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy228	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy230	=	buy229	    - Close[0]/100*steplevel1;GlobalVariableSet(			test230	,	buy230	);}
void	openbuy231	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy229	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy231	=	buy230	    - Close[0]/100*steplevel1;GlobalVariableSet(			test231	,	buy231	);}
void	openbuy232	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy230	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy232	=	buy231	    - Close[0]/100*steplevel1;GlobalVariableSet(			test232	,	buy232	);}
void	openbuy233	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy231	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy233	=	buy232	    - Close[0]/100*steplevel1;GlobalVariableSet(			test233	,	buy233	);}
void	openbuy234	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy232	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy234	=	buy233	    - Close[0]/100*steplevel1;GlobalVariableSet(			test234	,	buy234	);}
void	openbuy235	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy233	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy235	=	buy234	    - Close[0]/100*steplevel1;GlobalVariableSet(			test235	,	buy235	);}
void	openbuy236	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy234	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy236	=	buy235	    - Close[0]/100*steplevel1;GlobalVariableSet(			test236	,	buy236	);}
void	openbuy237	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy235	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy237	=	buy236	    - Close[0]/100*steplevel1;GlobalVariableSet(			test237	,	buy237	);}
void	openbuy238	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy236	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy238	=	buy237	    - Close[0]/100*steplevel1;GlobalVariableSet(			test238	,	buy238	);}
void	openbuy239	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy237	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy239	=	buy238	    - Close[0]/100*steplevel1;GlobalVariableSet(			test239	,	buy239	);}
void	openbuy240	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy238	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy240	=	buy239	    - Close[0]/100*steplevel1;GlobalVariableSet(			test240	,	buy240	);}
void	openbuy241	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy239	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy241	=	buy240	    - Close[0]/100*steplevel1;GlobalVariableSet(			test241	,	buy241	);}
void	openbuy242	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy240	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy242	=	buy241	    - Close[0]/100*steplevel1;GlobalVariableSet(			test242	,	buy242	);}
void	openbuy243	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy241	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy243	=	buy242	    - Close[0]/100*steplevel1;GlobalVariableSet(			test243	,	buy243	);}
void	openbuy244	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy242	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy244	=	buy243	    - Close[0]/100*steplevel1;GlobalVariableSet(			test244	,	buy244	);}
void	openbuy245	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy243	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy245	=	buy244	    - Close[0]/100*steplevel1;GlobalVariableSet(			test245	,	buy245	);}
void	openbuy246	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy244	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy246	=	buy245	    - Close[0]/100*steplevel1;GlobalVariableSet(			test246	,	buy246	);}
void	openbuy247	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy245	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy247	=	buy246	    - Close[0]/100*steplevel1;GlobalVariableSet(			test247	,	buy247	);}
void	openbuy248	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy246	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy248	=	buy247	    - Close[0]/100*steplevel1;GlobalVariableSet(			test248	,	buy248	);}
void	openbuy249	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy247	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy249	=	buy248	    - Close[0]/100*steplevel1;GlobalVariableSet(			test249	,	buy249	);}
void	openbuy250	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy248	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy250	=	buy249	    - Close[0]/100*steplevel1;GlobalVariableSet(			test250	,	buy250	);}
void	openbuy251	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy249	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy251	=	buy250	    - Close[0]/100*steplevel1;GlobalVariableSet(			test251	,	buy251	);}
void	openbuy252	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy250	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy252	=	buy251	    - Close[0]/100*steplevel1;GlobalVariableSet(			test252	,	buy252	);}
void	openbuy253	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy251	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy253	=	buy252	    - Close[0]/100*steplevel1;GlobalVariableSet(			test253	,	buy253	);}
void	openbuy254	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy252	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy254	=	buy253	    - Close[0]/100*steplevel1;GlobalVariableSet(			test254	,	buy254	);}
void	openbuy255	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy253	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy255	=	buy254	    - Close[0]/100*steplevel1;GlobalVariableSet(			test255	,	buy255	);}
void	openbuy256	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy254	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy256	=	buy255	    - Close[0]/100*steplevel1;GlobalVariableSet(			test256	,	buy256	);}
void	openbuy257	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy255	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy257	=	buy256	    - Close[0]/100*steplevel1;GlobalVariableSet(			test257	,	buy257	);}
void	openbuy258	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy256	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy258	=	buy257	    - Close[0]/100*steplevel1;GlobalVariableSet(			test258	,	buy258	);}
void	openbuy259	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy257	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy259	=	buy258	    - Close[0]/100*steplevel1;GlobalVariableSet(			test259	,	buy259	);}
void	openbuy260	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy258	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy260	=	buy259	    - Close[0]/100*steplevel1;GlobalVariableSet(			test260	,	buy260	);}
void	openbuy261	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy259	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy261	=	buy260	    - Close[0]/100*steplevel1;GlobalVariableSet(			test261	,	buy261	);}
void	openbuy262	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy260	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy262	=	buy261	    - Close[0]/100*steplevel1;GlobalVariableSet(			test262	,	buy262	);}
void	openbuy263	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy261	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy263	=	buy262	    - Close[0]/100*steplevel1;GlobalVariableSet(			test263	,	buy263	);}
void	openbuy264	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy262	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy264	=	buy263	    - Close[0]/100*steplevel1;GlobalVariableSet(			test264	,	buy264	);}
void	openbuy265	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy263	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy265	=	buy264	    - Close[0]/100*steplevel1;GlobalVariableSet(			test265	,	buy265	);}
void	openbuy266	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy264	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy266	=	buy265	    - Close[0]/100*steplevel1;GlobalVariableSet(			test266	,	buy266	);}
void	openbuy267	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy265	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy267	=	buy266	    - Close[0]/100*steplevel1;GlobalVariableSet(			test267	,	buy267	);}
void	openbuy268	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy266	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy268	=	buy267	    - Close[0]/100*steplevel1;GlobalVariableSet(			test268	,	buy268	);}
void	openbuy269	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy267	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy269	=	buy268	    - Close[0]/100*steplevel1;GlobalVariableSet(			test269	,	buy269	);}
void	openbuy270	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy268	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy270	=	buy269	    - Close[0]/100*steplevel1;GlobalVariableSet(			test270	,	buy270	);}
void	openbuy271	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy269	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy271	=	buy270	    - Close[0]/100*steplevel1;GlobalVariableSet(			test271	,	buy271	);}
void	openbuy272	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy270	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy272	=	buy271	    - Close[0]/100*steplevel1;GlobalVariableSet(			test272	,	buy272	);}
void	openbuy273	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy271	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy273	=	buy272	    - Close[0]/100*steplevel1;GlobalVariableSet(			test273	,	buy273	);}
void	openbuy274	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy272	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy274	=	buy273	    - Close[0]/100*steplevel1;GlobalVariableSet(			test274	,	buy274	);}
void	openbuy275	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy273	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy275	=	buy274	    - Close[0]/100*steplevel1;GlobalVariableSet(			test275	,	buy275	);}
void	openbuy276	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy274	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy276	=	buy275	    - Close[0]/100*steplevel1;GlobalVariableSet(			test276	,	buy276	);}
void	openbuy277	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy275	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy277	=	buy276	    - Close[0]/100*steplevel1;GlobalVariableSet(			test277	,	buy277	);}
void	openbuy278	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy276	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy278	=	buy277	    - Close[0]/100*steplevel1;GlobalVariableSet(			test278	,	buy278	);}
void	openbuy279	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy277	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy279	=	buy278	    - Close[0]/100*steplevel1;GlobalVariableSet(			test279	,	buy279	);}
void	openbuy280	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy278	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy280	=	buy279	    - Close[0]/100*steplevel1;GlobalVariableSet(			test280	,	buy280	);}
void	openbuy281	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy279	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy281	=	buy280	    - Close[0]/100*steplevel1;GlobalVariableSet(			test281	,	buy281	);}
void	openbuy282	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy280	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy282	=	buy281	    - Close[0]/100*steplevel1;GlobalVariableSet(			test282	,	buy282	);}
void	openbuy283	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy281	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy283	=	buy282	    - Close[0]/100*steplevel1;GlobalVariableSet(			test283	,	buy283	);}
void	openbuy284	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy282	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy284	=	buy283	    - Close[0]/100*steplevel1;GlobalVariableSet(			test284	,	buy284	);}
void	openbuy285	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy283	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy285	=	buy284	    - Close[0]/100*steplevel1;GlobalVariableSet(			test285	,	buy285	);}
void	openbuy286	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy284	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy286	=	buy285	    - Close[0]/100*steplevel1;GlobalVariableSet(			test286	,	buy286	);}
void	openbuy287	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy285	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy287	=	buy286	    - Close[0]/100*steplevel1;GlobalVariableSet(			test287	,	buy287	);}
void	openbuy288	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy286	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy288	=	buy287	    - Close[0]/100*steplevel1;GlobalVariableSet(			test288	,	buy288	);}
void	openbuy289	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy287	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy289	=	buy288	    - Close[0]/100*steplevel1;GlobalVariableSet(			test289	,	buy289	);}
void	openbuy290	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy288	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy290	=	buy289	    - Close[0]/100*steplevel1;GlobalVariableSet(			test290	,	buy290	);}
void	openbuy291	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy289	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy291	=	buy290	    - Close[0]/100*steplevel1;GlobalVariableSet(			test291	,	buy291	);}
void	openbuy292	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy290	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy292	=	buy291	    - Close[0]/100*steplevel1;GlobalVariableSet(			test292	,	buy292	);}
void	openbuy293	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy291	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy293	=	buy292	    - Close[0]/100*steplevel1;GlobalVariableSet(			test293	,	buy293	);}
void	openbuy294	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy292	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy294	=	buy293	    - Close[0]/100*steplevel1;GlobalVariableSet(			test294	,	buy294	);}
void	openbuy295	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy293	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy295	=	buy294	    - Close[0]/100*steplevel1;GlobalVariableSet(			test295	,	buy295	);}
void	openbuy296	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy294	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy296	=	buy295	    - Close[0]/100*steplevel1;GlobalVariableSet(			test296	,	buy296	);}
void	openbuy297	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy295	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy297	=	buy296	    - Close[0]/100*steplevel1;GlobalVariableSet(			test297	,	buy297	);}
void	openbuy298	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy296	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy298	=	buy297	    - Close[0]/100*steplevel1;GlobalVariableSet(			test298	,	buy298	);}
void	openbuy299	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy297	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy299	=	buy298	    - Close[0]/100*steplevel1;GlobalVariableSet(			test299	,	buy299	);}
void	openbuy300	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy298	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy300	=	buy299	    - Close[0]/100*steplevel1;GlobalVariableSet(			test300	,	buy300	);}
void	openbuy301	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy299	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy301	=	buy300	    - Close[0]/100*steplevel1;GlobalVariableSet(			test301	,	buy301	);}
void	openbuy302	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy300	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy302	=	buy301	    - Close[0]/100*steplevel1;GlobalVariableSet(			test302	,	buy302	);}
void	openbuy303	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy301	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy303	=	buy302	    - Close[0]/100*steplevel1;GlobalVariableSet(			test303	,	buy303	);}
void	openbuy304	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy302	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy304	=	buy303	    - Close[0]/100*steplevel1;GlobalVariableSet(			test304	,	buy304	);}
void	openbuy305	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy303	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy305	=	buy304	    - Close[0]/100*steplevel1;GlobalVariableSet(			test305	,	buy305	);}
void	openbuy306	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy304	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy306	=	buy305	    - Close[0]/100*steplevel1;GlobalVariableSet(			test306	,	buy306	);}
void	openbuy307	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy305	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy307	=	buy306	    - Close[0]/100*steplevel1;GlobalVariableSet(			test307	,	buy307	);}
void	openbuy308	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy306	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy308	=	buy307	    - Close[0]/100*steplevel1;GlobalVariableSet(			test308	,	buy308	);}
void	openbuy309	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy307	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy309	=	buy308	    - Close[0]/100*steplevel1;GlobalVariableSet(			test309	,	buy309	);}
void	openbuy310	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy308	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy310	=	buy309	    - Close[0]/100*steplevel1;GlobalVariableSet(			test310	,	buy310	);}
void	openbuy311	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy309	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy311	=	buy310	    - Close[0]/100*steplevel1;GlobalVariableSet(			test311	,	buy311	);}
void	openbuy312	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy310	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy312	=	buy311	    - Close[0]/100*steplevel1;GlobalVariableSet(			test312	,	buy312	);}
void	openbuy313	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy311	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy313	=	buy312	    - Close[0]/100*steplevel1;GlobalVariableSet(			test313	,	buy313	);}
void	openbuy314	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy312	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy314	=	buy313	    - Close[0]/100*steplevel1;GlobalVariableSet(			test314	,	buy314	);}
void	openbuy315	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy313	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy315	=	buy314	    - Close[0]/100*steplevel1;GlobalVariableSet(			test315	,	buy315	);}
void	openbuy316	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy314	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy316	=	buy315	    - Close[0]/100*steplevel1;GlobalVariableSet(			test316	,	buy316	);}
void	openbuy317	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy315	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy317	=	buy316	    - Close[0]/100*steplevel1;GlobalVariableSet(			test317	,	buy317	);}
void	openbuy318	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy316	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy318	=	buy317	    - Close[0]/100*steplevel1;GlobalVariableSet(			test318	,	buy318	);}
void	openbuy319	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy317	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy319	=	buy318	    - Close[0]/100*steplevel1;GlobalVariableSet(			test319	,	buy319	);}
void	openbuy320	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy318	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy320	=	buy319	    - Close[0]/100*steplevel1;GlobalVariableSet(			test320	,	buy320	);}
void	openbuy321	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy319	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy321	=	buy320	    - Close[0]/100*steplevel1;GlobalVariableSet(			test321	,	buy321	);}
void	openbuy322	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy320	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy322	=	buy321	    - Close[0]/100*steplevel1;GlobalVariableSet(			test322	,	buy322	);}
void	openbuy323	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy321	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy323	=	buy322	    - Close[0]/100*steplevel1;GlobalVariableSet(			test323	,	buy323	);}
void	openbuy324	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy322	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy324	=	buy323	    - Close[0]/100*steplevel1;GlobalVariableSet(			test324	,	buy324	);}
void	openbuy325	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy323	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy325	=	buy324	    - Close[0]/100*steplevel1;GlobalVariableSet(			test325	,	buy325	);}
void	openbuy326	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy324	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy326	=	buy325	    - Close[0]/100*steplevel1;GlobalVariableSet(			test326	,	buy326	);}
void	openbuy327	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy325	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy327	=	buy326	    - Close[0]/100*steplevel1;GlobalVariableSet(			test327	,	buy327	);}
void	openbuy328	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy326	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy328	=	buy327	    - Close[0]/100*steplevel1;GlobalVariableSet(			test328	,	buy328	);}
void	openbuy329	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy327	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy329	=	buy328	    - Close[0]/100*steplevel1;GlobalVariableSet(			test329	,	buy329	);}
void	openbuy330	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy328	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy330	=	buy329	    - Close[0]/100*steplevel1;GlobalVariableSet(			test330	,	buy330	);}
void	openbuy331	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy329	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy331	=	buy330	    - Close[0]/100*steplevel1;GlobalVariableSet(			test331	,	buy331	);}
void	openbuy332	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy330	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy332	=	buy331	    - Close[0]/100*steplevel1;GlobalVariableSet(			test332	,	buy332	);}
void	openbuy333	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy331	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy333	=	buy332	    - Close[0]/100*steplevel1;GlobalVariableSet(			test333	,	buy333	);}
void	openbuy334	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy332	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy334	=	buy333	    - Close[0]/100*steplevel1;GlobalVariableSet(			test334	,	buy334	);}
void	openbuy335	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy333	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy335	=	buy334	    - Close[0]/100*steplevel1;GlobalVariableSet(			test335	,	buy335	);}
void	openbuy336	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy334	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy336	=	buy335	    - Close[0]/100*steplevel1;GlobalVariableSet(			test336	,	buy336	);}
void	openbuy337	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy335	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy337	=	buy336	    - Close[0]/100*steplevel1;GlobalVariableSet(			test337	,	buy337	);}
void	openbuy338	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy336	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy338	=	buy337	    - Close[0]/100*steplevel1;GlobalVariableSet(			test338	,	buy338	);}
void	openbuy339	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy337	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy339	=	buy338	    - Close[0]/100*steplevel1;GlobalVariableSet(			test339	,	buy339	);}
void	openbuy340	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy338	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy340	=	buy339	    - Close[0]/100*steplevel1;GlobalVariableSet(			test340	,	buy340	);}
void	openbuy341	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy339	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy341	=	buy340	    - Close[0]/100*steplevel1;GlobalVariableSet(			test341	,	buy341	);}
void	openbuy342	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy340	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy342	=	buy341	    - Close[0]/100*steplevel1;GlobalVariableSet(			test342	,	buy342	);}
void	openbuy343	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy341	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy343	=	buy342	    - Close[0]/100*steplevel1;GlobalVariableSet(			test343	,	buy343	);}
void	openbuy344	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy342	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy344	=	buy343	    - Close[0]/100*steplevel1;GlobalVariableSet(			test344	,	buy344	);}
void	openbuy345	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy343	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy345	=	buy344	    - Close[0]/100*steplevel1;GlobalVariableSet(			test345	,	buy345	);}
void	openbuy346	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy344	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy346	=	buy345	    - Close[0]/100*steplevel1;GlobalVariableSet(			test346	,	buy346	);}
void	openbuy347	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy345	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy347	=	buy346	    - Close[0]/100*steplevel1;GlobalVariableSet(			test347	,	buy347	);}
void	openbuy348	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy346	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy348	=	buy347	    - Close[0]/100*steplevel1;GlobalVariableSet(			test348	,	buy348	);}
void	openbuy349	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy347	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy349	=	buy348	    - Close[0]/100*steplevel1;GlobalVariableSet(			test349	,	buy349	);}
void	openbuy350	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy348	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy350	=	buy349	    - Close[0]/100*steplevel1;GlobalVariableSet(			test350	,	buy350	);}
void	openbuy351	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy349	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy351	=	buy350	    - Close[0]/100*steplevel1;GlobalVariableSet(			test351	,	buy351	);}
void	openbuy352	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy350	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy352	=	buy351	    - Close[0]/100*steplevel1;GlobalVariableSet(			test352	,	buy352	);}
void	openbuy353	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy351	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy353	=	buy352	    - Close[0]/100*steplevel1;GlobalVariableSet(			test353	,	buy353	);}
void	openbuy354	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy352	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy354	=	buy353	    - Close[0]/100*steplevel1;GlobalVariableSet(			test354	,	buy354	);}
void	openbuy355	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy353	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy355	=	buy354	    - Close[0]/100*steplevel1;GlobalVariableSet(			test355	,	buy355	);}
void	openbuy356	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy354	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy356	=	buy355	    - Close[0]/100*steplevel1;GlobalVariableSet(			test356	,	buy356	);}
void	openbuy357	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy355	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy357	=	buy356	    - Close[0]/100*steplevel1;GlobalVariableSet(			test357	,	buy357	);}
void	openbuy358	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy356	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy358	=	buy357	    - Close[0]/100*steplevel1;GlobalVariableSet(			test358	,	buy358	);}
void	openbuy359	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy357	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy359	=	buy358	    - Close[0]/100*steplevel1;GlobalVariableSet(			test359	,	buy359	);}
void	openbuy360	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy358	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy360	=	buy359	    - Close[0]/100*steplevel1;GlobalVariableSet(			test360	,	buy360	);}
void	openbuy361	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy359	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy361	=	buy360	    - Close[0]/100*steplevel1;GlobalVariableSet(			test361	,	buy361	);}
void	openbuy362	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy360	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy362	=	buy361	    - Close[0]/100*steplevel1;GlobalVariableSet(			test362	,	buy362	);}
void	openbuy363	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy361	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy363	=	buy362	    - Close[0]/100*steplevel1;GlobalVariableSet(			test363	,	buy363	);}
void	openbuy364	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy362	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy364	=	buy363	    - Close[0]/100*steplevel1;GlobalVariableSet(			test364	,	buy364	);}
void	openbuy365	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy363	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy365	=	buy364	    - Close[0]/100*steplevel1;GlobalVariableSet(			test365	,	buy365	);}
void	openbuy366	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy364	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy366	=	buy365	    - Close[0]/100*steplevel1;GlobalVariableSet(			test366	,	buy366	);}
void	openbuy367	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy365	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy367	=	buy366	    - Close[0]/100*steplevel1;GlobalVariableSet(			test367	,	buy367	);}
void	openbuy368	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy366	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy368	=	buy367	    - Close[0]/100*steplevel1;GlobalVariableSet(			test368	,	buy368	);}
void	openbuy369	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy367	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy369	=	buy368	    - Close[0]/100*steplevel1;GlobalVariableSet(			test369	,	buy369	);}
void	openbuy370	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy368	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy370	=	buy369	    - Close[0]/100*steplevel1;GlobalVariableSet(			test370	,	buy370	);}
void	openbuy371	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy369	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy371	=	buy370	    - Close[0]/100*steplevel1;GlobalVariableSet(			test371	,	buy371	);}
void	openbuy372	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy370	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy372	=	buy371	    - Close[0]/100*steplevel1;GlobalVariableSet(			test372	,	buy372	);}
void	openbuy373	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy371	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy373	=	buy372	    - Close[0]/100*steplevel1;GlobalVariableSet(			test373	,	buy373	);}
void	openbuy374	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy372	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy374	=	buy373	    - Close[0]/100*steplevel1;GlobalVariableSet(			test374	,	buy374	);}
void	openbuy375	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy373	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy375	=	buy374	    - Close[0]/100*steplevel1;GlobalVariableSet(			test375	,	buy375	);}
void	openbuy376	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy374	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy376	=	buy375	    - Close[0]/100*steplevel1;GlobalVariableSet(			test376	,	buy376	);}
void	openbuy377	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy375	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy377	=	buy376	    - Close[0]/100*steplevel1;GlobalVariableSet(			test377	,	buy377	);}
void	openbuy378	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy376	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy378	=	buy377	    - Close[0]/100*steplevel1;GlobalVariableSet(			test378	,	buy378	);}
void	openbuy379	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy377	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy379	=	buy378	    - Close[0]/100*steplevel1;GlobalVariableSet(			test379	,	buy379	);}
void	openbuy380	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy378	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy380	=	buy379	    - Close[0]/100*steplevel1;GlobalVariableSet(			test380	,	buy380	);}
void	openbuy381	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy379	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy381	=	buy380	    - Close[0]/100*steplevel1;GlobalVariableSet(			test381	,	buy381	);}
void	openbuy382	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy380	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy382	=	buy381	    - Close[0]/100*steplevel1;GlobalVariableSet(			test382	,	buy382	);}
void	openbuy383	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy381	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy383	=	buy382	    - Close[0]/100*steplevel1;GlobalVariableSet(			test383	,	buy383	);}
void	openbuy384	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy382	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy384	=	buy383	    - Close[0]/100*steplevel1;GlobalVariableSet(			test384	,	buy384	);}
void	openbuy385	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy383	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy385	=	buy384	    - Close[0]/100*steplevel1;GlobalVariableSet(			test385	,	buy385	);}
void	openbuy386	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy384	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy386	=	buy385	    - Close[0]/100*steplevel1;GlobalVariableSet(			test386	,	buy386	);}
void	openbuy387	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy385	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy387	=	buy386	    - Close[0]/100*steplevel1;GlobalVariableSet(			test387	,	buy387	);}
void	openbuy388	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy386	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy388	=	buy387	    - Close[0]/100*steplevel1;GlobalVariableSet(			test388	,	buy388	);}
void	openbuy389	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy387	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy389	=	buy388	    - Close[0]/100*steplevel1;GlobalVariableSet(			test389	,	buy389	);}
void	openbuy390	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy388	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy390	=	buy389	    - Close[0]/100*steplevel1;GlobalVariableSet(			test390	,	buy390	);}
void	openbuy391	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy389	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy391	=	buy390	    - Close[0]/100*steplevel1;GlobalVariableSet(			test391	,	buy391	);}
void	openbuy392	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy390	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy392	=	buy391	    - Close[0]/100*steplevel1;GlobalVariableSet(			test392	,	buy392	);}
void	openbuy393	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy391	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy393	=	buy392	    - Close[0]/100*steplevel1;GlobalVariableSet(			test393	,	buy393	);}
void	openbuy394	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy392	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy394	=	buy393	    - Close[0]/100*steplevel1;GlobalVariableSet(			test394	,	buy394	);}
void	openbuy395	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy393	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy395	=	buy394	    - Close[0]/100*steplevel1;GlobalVariableSet(			test395	,	buy395	);}
void	openbuy396	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy394	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy396	=	buy395	    - Close[0]/100*steplevel1;GlobalVariableSet(			test396	,	buy396	);}
void	openbuy397	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy395	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy397	=	buy396	    - Close[0]/100*steplevel1;GlobalVariableSet(			test397	,	buy397	);}
void	openbuy398	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy396	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy398	=	buy397	    - Close[0]/100*steplevel1;GlobalVariableSet(			test398	,	buy398	);}
void	openbuy399	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy397	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy399	=	buy398	    - Close[0]/100*steplevel1;GlobalVariableSet(			test399	,	buy399	);}
void	openbuy400	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy398	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy400	=	buy399	    - Close[0]/100*steplevel1;GlobalVariableSet(			test400	,	buy400	);}
void	openbuy401	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy399	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy401	=	buy400	    - Close[0]/100*steplevel1;GlobalVariableSet(			test401	,	buy401	);}
void	openbuy402	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy400	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy402	=	buy401	    - Close[0]/100*steplevel1;GlobalVariableSet(			test402	,	buy402	);}
void	openbuy403	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy401	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy403	=	buy402	    - Close[0]/100*steplevel1;GlobalVariableSet(			test403	,	buy403	);}
void	openbuy404	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy402	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy404	=	buy403	    - Close[0]/100*steplevel1;GlobalVariableSet(			test404	,	buy404	);}
void	openbuy405	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy403	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy405	=	buy404	    - Close[0]/100*steplevel1;GlobalVariableSet(			test405	,	buy405	);}
void	openbuy406	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy404	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy406	=	buy405	    - Close[0]/100*steplevel1;GlobalVariableSet(			test406	,	buy406	);}
void	openbuy407	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy405	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy407	=	buy406	    - Close[0]/100*steplevel1;GlobalVariableSet(			test407	,	buy407	);}
void	openbuy408	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy406	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy408	=	buy407	    - Close[0]/100*steplevel1;GlobalVariableSet(			test408	,	buy408	);}
void	openbuy409	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy407	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy409	=	buy408	    - Close[0]/100*steplevel1;GlobalVariableSet(			test409	,	buy409	);}
void	openbuy410	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy408	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy410	=	buy409	    - Close[0]/100*steplevel1;GlobalVariableSet(			test410	,	buy410	);}
void	openbuy411	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy409	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy411	=	buy410	    - Close[0]/100*steplevel1;GlobalVariableSet(			test411	,	buy411	);}
void	openbuy412	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy410	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy412	=	buy411	    - Close[0]/100*steplevel1;GlobalVariableSet(			test412	,	buy412	);}
void	openbuy413	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy411	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy413	=	buy412	    - Close[0]/100*steplevel1;GlobalVariableSet(			test413	,	buy413	);}
void	openbuy414	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy412	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy414	=	buy413	    - Close[0]/100*steplevel1;GlobalVariableSet(			test414	,	buy414	);}
void	openbuy415	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy413	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy415	=	buy414	    - Close[0]/100*steplevel1;GlobalVariableSet(			test415	,	buy415	);}
void	openbuy416	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy414	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy416	=	buy415	    - Close[0]/100*steplevel1;GlobalVariableSet(			test416	,	buy416	);}
void	openbuy417	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy415	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy417	=	buy416	    - Close[0]/100*steplevel1;GlobalVariableSet(			test417	,	buy417	);}
void	openbuy418	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy416	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy418	=	buy417	    - Close[0]/100*steplevel1;GlobalVariableSet(			test418	,	buy418	);}
void	openbuy419	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy417	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy419	=	buy418	    - Close[0]/100*steplevel1;GlobalVariableSet(			test419	,	buy419	);}
void	openbuy420	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy418	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy420	=	buy419	    - Close[0]/100*steplevel1;GlobalVariableSet(			test420	,	buy420	);}
void	openbuy421	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy419	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy421	=	buy420	    - Close[0]/100*steplevel1;GlobalVariableSet(			test421	,	buy421	);}
void	openbuy422	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy420	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy422	=	buy421	    - Close[0]/100*steplevel1;GlobalVariableSet(			test422	,	buy422	);}
void	openbuy423	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy421	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy423	=	buy422	    - Close[0]/100*steplevel1;GlobalVariableSet(			test423	,	buy423	);}
void	openbuy424	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy422	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy424	=	buy423	    - Close[0]/100*steplevel1;GlobalVariableSet(			test424	,	buy424	);}
void	openbuy425	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy423	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy425	=	buy424	    - Close[0]/100*steplevel1;GlobalVariableSet(			test425	,	buy425	);}
void	openbuy426	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy424	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy426	=	buy425	    - Close[0]/100*steplevel1;GlobalVariableSet(			test426	,	buy426	);}
void	openbuy427	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy425	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy427	=	buy426	    - Close[0]/100*steplevel1;GlobalVariableSet(			test427	,	buy427	);}
void	openbuy428	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy426	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy428	=	buy427	    - Close[0]/100*steplevel1;GlobalVariableSet(			test428	,	buy428	);}
void	openbuy429	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy427	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy429	=	buy428	    - Close[0]/100*steplevel1;GlobalVariableSet(			test429	,	buy429	);}
void	openbuy430	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy428	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy430	=	buy429	    - Close[0]/100*steplevel1;GlobalVariableSet(			test430	,	buy430	);}
void	openbuy431	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy429	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy431	=	buy430	    - Close[0]/100*steplevel1;GlobalVariableSet(			test431	,	buy431	);}
void	openbuy432	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy430	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy432	=	buy431	    - Close[0]/100*steplevel1;GlobalVariableSet(			test432	,	buy432	);}
void	openbuy433	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy431	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy433	=	buy432	    - Close[0]/100*steplevel1;GlobalVariableSet(			test433	,	buy433	);}
void	openbuy434	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy432	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy434	=	buy433	    - Close[0]/100*steplevel1;GlobalVariableSet(			test434	,	buy434	);}
void	openbuy435	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy433	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy435	=	buy434	    - Close[0]/100*steplevel1;GlobalVariableSet(			test435	,	buy435	);}
void	openbuy436	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy434	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy436	=	buy435	    - Close[0]/100*steplevel1;GlobalVariableSet(			test436	,	buy436	);}
void	openbuy437	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy435	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy437	=	buy436	    - Close[0]/100*steplevel1;GlobalVariableSet(			test437	,	buy437	);}
void	openbuy438	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy436	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy438	=	buy437	    - Close[0]/100*steplevel1;GlobalVariableSet(			test438	,	buy438	);}
void	openbuy439	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy437	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy439	=	buy438	    - Close[0]/100*steplevel1;GlobalVariableSet(			test439	,	buy439	);}
void	openbuy440	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy438	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy440	=	buy439	    - Close[0]/100*steplevel1;GlobalVariableSet(			test440	,	buy440	);}
void	openbuy441	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy439	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy441	=	buy440	    - Close[0]/100*steplevel1;GlobalVariableSet(			test441	,	buy441	);}
void	openbuy442	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy440	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy442	=	buy441	    - Close[0]/100*steplevel1;GlobalVariableSet(			test442	,	buy442	);}
void	openbuy443	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy441	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy443	=	buy442	    - Close[0]/100*steplevel1;GlobalVariableSet(			test443	,	buy443	);}
void	openbuy444	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy442	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy444	=	buy443	    - Close[0]/100*steplevel1;GlobalVariableSet(			test444	,	buy444	);}
void	openbuy445	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy443	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy445	=	buy444	    - Close[0]/100*steplevel1;GlobalVariableSet(			test445	,	buy445	);}
void	openbuy446	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy444	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy446	=	buy445	    - Close[0]/100*steplevel1;GlobalVariableSet(			test446	,	buy446	);}
void	openbuy447	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy445	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy447	=	buy446	    - Close[0]/100*steplevel1;GlobalVariableSet(			test447	,	buy447	);}
void	openbuy448	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy446	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy448	=	buy447	    - Close[0]/100*steplevel1;GlobalVariableSet(			test448	,	buy448	);}
void	openbuy449	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy447	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy449	=	buy448	    - Close[0]/100*steplevel1;GlobalVariableSet(			test449	,	buy449	);}
void	openbuy450	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy448	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy450	=	buy449	    - Close[0]/100*steplevel1;GlobalVariableSet(			test450	,	buy450	);}
void	openbuy451	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy449	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy451	=	buy450	    - Close[0]/100*steplevel1;GlobalVariableSet(			test451	,	buy451	);}
void	openbuy452	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy450	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy452	=	buy451	    - Close[0]/100*steplevel1;GlobalVariableSet(			test452	,	buy452	);}
void	openbuy453	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy451	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy453	=	buy452	    - Close[0]/100*steplevel1;GlobalVariableSet(			test453	,	buy453	);}
void	openbuy454	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy452	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy454	=	buy453	    - Close[0]/100*steplevel1;GlobalVariableSet(			test454	,	buy454	);}
void	openbuy455	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy453	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy455	=	buy454	    - Close[0]/100*steplevel1;GlobalVariableSet(			test455	,	buy455	);}
void	openbuy456	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy454	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy456	=	buy455	    - Close[0]/100*steplevel1;GlobalVariableSet(			test456	,	buy456	);}
void	openbuy457	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy455	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy457	=	buy456	    - Close[0]/100*steplevel1;GlobalVariableSet(			test457	,	buy457	);}
void	openbuy458	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy456	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy458	=	buy457	    - Close[0]/100*steplevel1;GlobalVariableSet(			test458	,	buy458	);}
void	openbuy459	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy457	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy459	=	buy458	    - Close[0]/100*steplevel1;GlobalVariableSet(			test459	,	buy459	);}
void	openbuy460	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy458	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy460	=	buy459	    - Close[0]/100*steplevel1;GlobalVariableSet(			test460	,	buy460	);}
void	openbuy461	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy459	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy461	=	buy460	    - Close[0]/100*steplevel1;GlobalVariableSet(			test461	,	buy461	);}
void	openbuy462	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy460	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy462	=	buy461	    - Close[0]/100*steplevel1;GlobalVariableSet(			test462	,	buy462	);}
void	openbuy463	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy461	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy463	=	buy462	    - Close[0]/100*steplevel1;GlobalVariableSet(			test463	,	buy463	);}
void	openbuy464	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy462	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy464	=	buy463	    - Close[0]/100*steplevel1;GlobalVariableSet(			test464	,	buy464	);}
void	openbuy465	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy463	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy465	=	buy464	    - Close[0]/100*steplevel1;GlobalVariableSet(			test465	,	buy465	);}
void	openbuy466	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy464	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy466	=	buy465	    - Close[0]/100*steplevel1;GlobalVariableSet(			test466	,	buy466	);}
void	openbuy467	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy465	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy467	=	buy466	    - Close[0]/100*steplevel1;GlobalVariableSet(			test467	,	buy467	);}
void	openbuy468	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy466	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy468	=	buy467	    - Close[0]/100*steplevel1;GlobalVariableSet(			test468	,	buy468	);}
void	openbuy469	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy467	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy469	=	buy468	    - Close[0]/100*steplevel1;GlobalVariableSet(			test469	,	buy469	);}
void	openbuy470	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy468	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy470	=	buy469	    - Close[0]/100*steplevel1;GlobalVariableSet(			test470	,	buy470	);}
void	openbuy471	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy469	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy471	=	buy470	    - Close[0]/100*steplevel1;GlobalVariableSet(			test471	,	buy471	);}
void	openbuy472	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy470	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy472	=	buy471	    - Close[0]/100*steplevel1;GlobalVariableSet(			test472	,	buy472	);}
void	openbuy473	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy471	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy473	=	buy472	    - Close[0]/100*steplevel1;GlobalVariableSet(			test473	,	buy473	);}
void	openbuy474	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy472	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy474	=	buy473	    - Close[0]/100*steplevel1;GlobalVariableSet(			test474	,	buy474	);}
void	openbuy475	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy473	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy475	=	buy474	    - Close[0]/100*steplevel1;GlobalVariableSet(			test475	,	buy475	);}
void	openbuy476	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy474	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy476	=	buy475	    - Close[0]/100*steplevel1;GlobalVariableSet(			test476	,	buy476	);}
void	openbuy477	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy475	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy477	=	buy476	    - Close[0]/100*steplevel1;GlobalVariableSet(			test477	,	buy477	);}
void	openbuy478	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy476	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy478	=	buy477	    - Close[0]/100*steplevel1;GlobalVariableSet(			test478	,	buy478	);}
void	openbuy479	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy477	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy479	=	buy478	    - Close[0]/100*steplevel1;GlobalVariableSet(			test479	,	buy479	);}
void	openbuy480	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy478	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy480	=	buy479	    - Close[0]/100*steplevel1;GlobalVariableSet(			test480	,	buy480	);}
void	openbuy481	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy479	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy481	=	buy480	    - Close[0]/100*steplevel1;GlobalVariableSet(			test481	,	buy481	);}
void	openbuy482	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy480	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy482	=	buy481	    - Close[0]/100*steplevel1;GlobalVariableSet(			test482	,	buy482	);}
void	openbuy483	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy481	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy483	=	buy482	    - Close[0]/100*steplevel1;GlobalVariableSet(			test483	,	buy483	);}
void	openbuy484	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy482	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy484	=	buy483	    - Close[0]/100*steplevel1;GlobalVariableSet(			test484	,	buy484	);}
void	openbuy485	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy483	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy485	=	buy484	    - Close[0]/100*steplevel1;GlobalVariableSet(			test485	,	buy485	);}
void	openbuy486	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy484	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy486	=	buy485	    - Close[0]/100*steplevel1;GlobalVariableSet(			test486	,	buy486	);}
void	openbuy487	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy485	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy487	=	buy486	    - Close[0]/100*steplevel1;GlobalVariableSet(			test487	,	buy487	);}
void	openbuy488	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy486	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy488	=	buy487	    - Close[0]/100*steplevel1;GlobalVariableSet(			test488	,	buy488	);}
void	openbuy489	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy487	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy489	=	buy488	    - Close[0]/100*steplevel1;GlobalVariableSet(			test489	,	buy489	);}
void	openbuy490	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy488	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy490	=	buy489	    - Close[0]/100*steplevel1;GlobalVariableSet(			test490	,	buy490	);}
void	openbuy491	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy489	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy491	=	buy490	    - Close[0]/100*steplevel1;GlobalVariableSet(			test491	,	buy491	);}
void	openbuy492	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy490	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy492	=	buy491	    - Close[0]/100*steplevel1;GlobalVariableSet(			test492	,	buy492	);}
void	openbuy493	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy491	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy493	=	buy492	    - Close[0]/100*steplevel1;GlobalVariableSet(			test493	,	buy493	);}
void	openbuy494	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy492	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy494	=	buy493	    - Close[0]/100*steplevel1;GlobalVariableSet(			test494	,	buy494	);}
void	openbuy495	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy493	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy495	=	buy494	    - Close[0]/100*steplevel1;GlobalVariableSet(			test495	,	buy495	);}
void	openbuy496	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy494	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy496	=	buy495	    - Close[0]/100*steplevel1;GlobalVariableSet(			test496	,	buy496	);}
void	openbuy497	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy495	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy497	=	buy496	    - Close[0]/100*steplevel1;GlobalVariableSet(			test497	,	buy497	);}
void	openbuy498	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy496	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy498	=	buy497	    - Close[0]/100*steplevel1;GlobalVariableSet(			test498	,	buy498	);}
void	openbuy499	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy497	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy499	=	buy498	    - Close[0]/100*steplevel1;GlobalVariableSet(			test499	,	buy499	);}
void	openbuy500	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy498	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy500	=	buy499	    - Close[0]/100*steplevel1;GlobalVariableSet(			test500	,	buy500	);}
void	openbuy501	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy499	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy501	=	buy500	    - Close[0]/100*steplevel1;GlobalVariableSet(			test501	,	buy501	);}
void	openbuy502	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy500	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy502	=	buy501	    - Close[0]/100*steplevel1;GlobalVariableSet(			test502	,	buy502	);}
void	openbuy503	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy501	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy503	=	buy502	    - Close[0]/100*steplevel1;GlobalVariableSet(			test503	,	buy503	);}
void	openbuy504	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy502	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy504	=	buy503	    - Close[0]/100*steplevel1;GlobalVariableSet(			test504	,	buy504	);}
void	openbuy505	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy503	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy505	=	buy504	    - Close[0]/100*steplevel1;GlobalVariableSet(			test505	,	buy505	);}
void	openbuy506	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy504	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy506	=	buy505	    - Close[0]/100*steplevel1;GlobalVariableSet(			test506	,	buy506	);}
void	openbuy507	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy505	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy507	=	buy506	    - Close[0]/100*steplevel1;GlobalVariableSet(			test507	,	buy507	);}
void	openbuy508	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy506	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy508	=	buy507	    - Close[0]/100*steplevel1;GlobalVariableSet(			test508	,	buy508	);}
void	openbuy509	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy507	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy509	=	buy508	    - Close[0]/100*steplevel1;GlobalVariableSet(			test509	,	buy509	);}
void	openbuy510	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy508	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy510	=	buy509	    - Close[0]/100*steplevel1;GlobalVariableSet(			test510	,	buy510	);}
void	openbuy511	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy509	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy511	=	buy510	    - Close[0]/100*steplevel1;GlobalVariableSet(			test511	,	buy511	);}
void	openbuy512	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy510	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy512	=	buy511	    - Close[0]/100*steplevel1;GlobalVariableSet(			test512	,	buy512	);}
void	openbuy513	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy511	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy513	=	buy512	    - Close[0]/100*steplevel1;GlobalVariableSet(			test513	,	buy513	);}
void	openbuy514	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy512	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy514	=	buy513	    - Close[0]/100*steplevel1;GlobalVariableSet(			test514	,	buy514	);}
void	openbuy515	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy513	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy515	=	buy514	    - Close[0]/100*steplevel1;GlobalVariableSet(			test515	,	buy515	);}
void	openbuy516	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy514	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy516	=	buy515	    - Close[0]/100*steplevel1;GlobalVariableSet(			test516	,	buy516	);}
void	openbuy517	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy515	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy517	=	buy516	    - Close[0]/100*steplevel1;GlobalVariableSet(			test517	,	buy517	);}
void	openbuy518	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy516	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy518	=	buy517	    - Close[0]/100*steplevel1;GlobalVariableSet(			test518	,	buy518	);}
void	openbuy519	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy517	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy519	=	buy518	    - Close[0]/100*steplevel1;GlobalVariableSet(			test519	,	buy519	);}
void	openbuy520	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy518	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy520	=	buy519	    - Close[0]/100*steplevel1;GlobalVariableSet(			test520	,	buy520	);}
void	openbuy521	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy519	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy521	=	buy520	    - Close[0]/100*steplevel1;GlobalVariableSet(			test521	,	buy521	);}
void	openbuy522	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy520	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy522	=	buy521	    - Close[0]/100*steplevel1;GlobalVariableSet(			test522	,	buy522	);}
void	openbuy523	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy521	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy523	=	buy522	    - Close[0]/100*steplevel1;GlobalVariableSet(			test523	,	buy523	);}
void	openbuy524	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy522	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy524	=	buy523	    - Close[0]/100*steplevel1;GlobalVariableSet(			test524	,	buy524	);}
void	openbuy525	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy523	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy525	=	buy524	    - Close[0]/100*steplevel1;GlobalVariableSet(			test525	,	buy525	);}
void	openbuy526	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy524	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy526	=	buy525	    - Close[0]/100*steplevel1;GlobalVariableSet(			test526	,	buy526	);}
void	openbuy527	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy525	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy527	=	buy526	    - Close[0]/100*steplevel1;GlobalVariableSet(			test527	,	buy527	);}
void	openbuy528	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy526	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy528	=	buy527	    - Close[0]/100*steplevel1;GlobalVariableSet(			test528	,	buy528	);}
void	openbuy529	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy527	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy529	=	buy528	    - Close[0]/100*steplevel1;GlobalVariableSet(			test529	,	buy529	);}
void	openbuy530	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy528	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy530	=	buy529	    - Close[0]/100*steplevel1;GlobalVariableSet(			test530	,	buy530	);}
void	openbuy531	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy529	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy531	=	buy530	    - Close[0]/100*steplevel1;GlobalVariableSet(			test531	,	buy531	);}
void	openbuy532	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy530	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy532	=	buy531	    - Close[0]/100*steplevel1;GlobalVariableSet(			test532	,	buy532	);}
void	openbuy533	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy531	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy533	=	buy532	    - Close[0]/100*steplevel1;GlobalVariableSet(			test533	,	buy533	);}
void	openbuy534	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy532	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy534	=	buy533	    - Close[0]/100*steplevel1;GlobalVariableSet(			test534	,	buy534	);}
void	openbuy535	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy533	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy535	=	buy534	    - Close[0]/100*steplevel1;GlobalVariableSet(			test535	,	buy535	);}
void	openbuy536	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy534	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy536	=	buy535	    - Close[0]/100*steplevel1;GlobalVariableSet(			test536	,	buy536	);}
void	openbuy537	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy535	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy537	=	buy536	    - Close[0]/100*steplevel1;GlobalVariableSet(			test537	,	buy537	);}
void	openbuy538	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy536	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy538	=	buy537	    - Close[0]/100*steplevel1;GlobalVariableSet(			test538	,	buy538	);}
void	openbuy539	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy537	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy539	=	buy538	    - Close[0]/100*steplevel1;GlobalVariableSet(			test539	,	buy539	);}
void	openbuy540	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy538	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy540	=	buy539	    - Close[0]/100*steplevel1;GlobalVariableSet(			test540	,	buy540	);}
void	openbuy541	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy539	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy541	=	buy540	    - Close[0]/100*steplevel1;GlobalVariableSet(			test541	,	buy541	);}
void	openbuy542	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy540	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy542	=	buy541	    - Close[0]/100*steplevel1;GlobalVariableSet(			test542	,	buy542	);}
void	openbuy543	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy541	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy543	=	buy542	    - Close[0]/100*steplevel1;GlobalVariableSet(			test543	,	buy543	);}
void	openbuy544	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy542	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy544	=	buy543	    - Close[0]/100*steplevel1;GlobalVariableSet(			test544	,	buy544	);}
void	openbuy545	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy543	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy545	=	buy544	    - Close[0]/100*steplevel1;GlobalVariableSet(			test545	,	buy545	);}
void	openbuy546	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy544	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy546	=	buy545	    - Close[0]/100*steplevel1;GlobalVariableSet(			test546	,	buy546	);}
void	openbuy547	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy545	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy547	=	buy546	    - Close[0]/100*steplevel1;GlobalVariableSet(			test547	,	buy547	);}
void	openbuy548	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy546	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy548	=	buy547	    - Close[0]/100*steplevel1;GlobalVariableSet(			test548	,	buy548	);}
void	openbuy549	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy547	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy549	=	buy548	    - Close[0]/100*steplevel1;GlobalVariableSet(			test549	,	buy549	);}
void	openbuy550	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy548	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy550	=	buy549	    - Close[0]/100*steplevel1;GlobalVariableSet(			test550	,	buy550	);}
void	openbuy551	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy549	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy551	=	buy550	    - Close[0]/100*steplevel1;GlobalVariableSet(			test551	,	buy551	);}
void	openbuy552	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy550	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy552	=	buy551	    - Close[0]/100*steplevel1;GlobalVariableSet(			test552	,	buy552	);}
void	openbuy553	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy551	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy553	=	buy552	    - Close[0]/100*steplevel1;GlobalVariableSet(			test553	,	buy553	);}
void	openbuy554	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy552	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy554	=	buy553	    - Close[0]/100*steplevel1;GlobalVariableSet(			test554	,	buy554	);}
void	openbuy555	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy553	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy555	=	buy554	    - Close[0]/100*steplevel1;GlobalVariableSet(			test555	,	buy555	);}
void	openbuy556	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy554	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy556	=	buy555	    - Close[0]/100*steplevel1;GlobalVariableSet(			test556	,	buy556	);}
void	openbuy557	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy555	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy557	=	buy556	    - Close[0]/100*steplevel1;GlobalVariableSet(			test557	,	buy557	);}
void	openbuy558	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy556	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy558	=	buy557	    - Close[0]/100*steplevel1;GlobalVariableSet(			test558	,	buy558	);}
void	openbuy559	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy557	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy559	=	buy558	    - Close[0]/100*steplevel1;GlobalVariableSet(			test559	,	buy559	);}
void	openbuy560	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy558	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy560	=	buy559	    - Close[0]/100*steplevel1;GlobalVariableSet(			test560	,	buy560	);}
void	openbuy561	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy559	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy561	=	buy560	    - Close[0]/100*steplevel1;GlobalVariableSet(			test561	,	buy561	);}
void	openbuy562	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy560	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy562	=	buy561	    - Close[0]/100*steplevel1;GlobalVariableSet(			test562	,	buy562	);}
void	openbuy563	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy561	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy563	=	buy562	    - Close[0]/100*steplevel1;GlobalVariableSet(			test563	,	buy563	);}
void	openbuy564	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy562	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy564	=	buy563	    - Close[0]/100*steplevel1;GlobalVariableSet(			test564	,	buy564	);}
void	openbuy565	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy563	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy565	=	buy564	    - Close[0]/100*steplevel1;GlobalVariableSet(			test565	,	buy565	);}
void	openbuy566	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy564	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy566	=	buy565	    - Close[0]/100*steplevel1;GlobalVariableSet(			test566	,	buy566	);}
void	openbuy567	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy565	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy567	=	buy566	    - Close[0]/100*steplevel1;GlobalVariableSet(			test567	,	buy567	);}
void	openbuy568	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy566	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy568	=	buy567	    - Close[0]/100*steplevel1;GlobalVariableSet(			test568	,	buy568	);}
void	openbuy569	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy567	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy569	=	buy568	    - Close[0]/100*steplevel1;GlobalVariableSet(			test569	,	buy569	);}
void	openbuy570	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy568	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy570	=	buy569	    - Close[0]/100*steplevel1;GlobalVariableSet(			test570	,	buy570	);}
void	openbuy571	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy569	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy571	=	buy570	    - Close[0]/100*steplevel1;GlobalVariableSet(			test571	,	buy571	);}
void	openbuy572	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy570	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy572	=	buy571	    - Close[0]/100*steplevel1;GlobalVariableSet(			test572	,	buy572	);}
void	openbuy573	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy571	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy573	=	buy572	    - Close[0]/100*steplevel1;GlobalVariableSet(			test573	,	buy573	);}
void	openbuy574	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy572	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy574	=	buy573	    - Close[0]/100*steplevel1;GlobalVariableSet(			test574	,	buy574	);}
void	openbuy575	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy573	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy575	=	buy574	    - Close[0]/100*steplevel1;GlobalVariableSet(			test575	,	buy575	);}
void	openbuy576	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy574	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy576	=	buy575	    - Close[0]/100*steplevel1;GlobalVariableSet(			test576	,	buy576	);}
void	openbuy577	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy575	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy577	=	buy576	    - Close[0]/100*steplevel1;GlobalVariableSet(			test577	,	buy577	);}
void	openbuy578	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy576	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy578	=	buy577	    - Close[0]/100*steplevel1;GlobalVariableSet(			test578	,	buy578	);}
void	openbuy579	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy577	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy579	=	buy578	    - Close[0]/100*steplevel1;GlobalVariableSet(			test579	,	buy579	);}
void	openbuy580	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy578	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy580	=	buy579	    - Close[0]/100*steplevel1;GlobalVariableSet(			test580	,	buy580	);}
void	openbuy581	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy579	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy581	=	buy580	    - Close[0]/100*steplevel1;GlobalVariableSet(			test581	,	buy581	);}
void	openbuy582	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy580	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy582	=	buy581	    - Close[0]/100*steplevel1;GlobalVariableSet(			test582	,	buy582	);}
void	openbuy583	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy581	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy583	=	buy582	    - Close[0]/100*steplevel1;GlobalVariableSet(			test583	,	buy583	);}
void	openbuy584	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy582	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy584	=	buy583	    - Close[0]/100*steplevel1;GlobalVariableSet(			test584	,	buy584	);}
void	openbuy585	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy583	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy585	=	buy584	    - Close[0]/100*steplevel1;GlobalVariableSet(			test585	,	buy585	);}
void	openbuy586	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy584	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy586	=	buy585	    - Close[0]/100*steplevel1;GlobalVariableSet(			test586	,	buy586	);}
void	openbuy587	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy585	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy587	=	buy586	    - Close[0]/100*steplevel1;GlobalVariableSet(			test587	,	buy587	);}
void	openbuy588	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy586	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy588	=	buy587	    - Close[0]/100*steplevel1;GlobalVariableSet(			test588	,	buy588	);}
void	openbuy589	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy587	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy589	=	buy588	    - Close[0]/100*steplevel1;GlobalVariableSet(			test589	,	buy589	);}
void	openbuy590	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy588	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy590	=	buy589	    - Close[0]/100*steplevel1;GlobalVariableSet(			test590	,	buy590	);}
void	openbuy591	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy589	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy591	=	buy590	    - Close[0]/100*steplevel1;GlobalVariableSet(			test591	,	buy591	);}
void	openbuy592	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy590	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy592	=	buy591	    - Close[0]/100*steplevel1;GlobalVariableSet(			test592	,	buy592	);}
void	openbuy593	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy591	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy593	=	buy592	    - Close[0]/100*steplevel1;GlobalVariableSet(			test593	,	buy593	);}
void	openbuy594	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy592	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy594	=	buy593	    - Close[0]/100*steplevel1;GlobalVariableSet(			test594	,	buy594	);}
void	openbuy595	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy593	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy595	=	buy594	    - Close[0]/100*steplevel1;GlobalVariableSet(			test595	,	buy595	);}
void	openbuy596	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy594	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy596	=	buy595	    - Close[0]/100*steplevel1;GlobalVariableSet(			test596	,	buy596	);}
void	openbuy597	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy595	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy597	=	buy596	    - Close[0]/100*steplevel1;GlobalVariableSet(			test597	,	buy597	);}
void	openbuy598	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy596	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy598	=	buy597	    - Close[0]/100*steplevel1;GlobalVariableSet(			test598	,	buy598	);}
void	openbuy599	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy597	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy599	=	buy598	    - Close[0]/100*steplevel1;GlobalVariableSet(			test599	,	buy599	);}
void	openbuy600	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy598	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy600	=	buy599	    - Close[0]/100*steplevel1;GlobalVariableSet(			test600	,	buy600	);}
void	openbuy601	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy599	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy601	=	buy600	    - Close[0]/100*steplevel1;GlobalVariableSet(			test601	,	buy601	);}
void	openbuy602	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy600	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy602	=	buy601	    - Close[0]/100*steplevel1;GlobalVariableSet(			test602	,	buy602	);}
void	openbuy603	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy601	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy603	=	buy602	    - Close[0]/100*steplevel1;GlobalVariableSet(			test603	,	buy603	);}
void	openbuy604	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy602	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy604	=	buy603	    - Close[0]/100*steplevel1;GlobalVariableSet(			test604	,	buy604	);}
void	openbuy605	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy603	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy605	=	buy604	    - Close[0]/100*steplevel1;GlobalVariableSet(			test605	,	buy605	);}
void	openbuy606	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy604	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy606	=	buy605	    - Close[0]/100*steplevel1;GlobalVariableSet(			test606	,	buy606	);}
void	openbuy607	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy605	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy607	=	buy606	    - Close[0]/100*steplevel1;GlobalVariableSet(			test607	,	buy607	);}
void	openbuy608	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy606	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy608	=	buy607	    - Close[0]/100*steplevel1;GlobalVariableSet(			test608	,	buy608	);}
void	openbuy609	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy607	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy609	=	buy608	    - Close[0]/100*steplevel1;GlobalVariableSet(			test609	,	buy609	);}
void	openbuy610	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy608	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy610	=	buy609	    - Close[0]/100*steplevel1;GlobalVariableSet(			test610	,	buy610	);}
void	openbuy611	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy609	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy611	=	buy610	    - Close[0]/100*steplevel1;GlobalVariableSet(			test611	,	buy611	);}
void	openbuy612	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy610	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy612	=	buy611	    - Close[0]/100*steplevel1;GlobalVariableSet(			test612	,	buy612	);}
void	openbuy613	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy611	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy613	=	buy612	    - Close[0]/100*steplevel1;GlobalVariableSet(			test613	,	buy613	);}
void	openbuy614	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy612	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy614	=	buy613	    - Close[0]/100*steplevel1;GlobalVariableSet(			test614	,	buy614	);}
void	openbuy615	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy613	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy615	=	buy614	    - Close[0]/100*steplevel1;GlobalVariableSet(			test615	,	buy615	);}
void	openbuy616	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy614	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy616	=	buy615	    - Close[0]/100*steplevel1;GlobalVariableSet(			test616	,	buy616	);}
void	openbuy617	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy615	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy617	=	buy616	    - Close[0]/100*steplevel1;GlobalVariableSet(			test617	,	buy617	);}
void	openbuy618	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy616	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy618	=	buy617	    - Close[0]/100*steplevel1;GlobalVariableSet(			test618	,	buy618	);}
void	openbuy619	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy617	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy619	=	buy618	    - Close[0]/100*steplevel1;GlobalVariableSet(			test619	,	buy619	);}
void	openbuy620	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy618	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy620	=	buy619	    - Close[0]/100*steplevel1;GlobalVariableSet(			test620	,	buy620	);}
void	openbuy621	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy619	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy621	=	buy620	    - Close[0]/100*steplevel1;GlobalVariableSet(			test621	,	buy621	);}
void	openbuy622	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy620	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy622	=	buy621	    - Close[0]/100*steplevel1;GlobalVariableSet(			test622	,	buy622	);}
void	openbuy623	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy621	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy623	=	buy622	    - Close[0]/100*steplevel1;GlobalVariableSet(			test623	,	buy623	);}
void	openbuy624	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy622	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy624	=	buy623	    - Close[0]/100*steplevel1;GlobalVariableSet(			test624	,	buy624	);}
void	openbuy625	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy623	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy625	=	buy624	    - Close[0]/100*steplevel1;GlobalVariableSet(			test625	,	buy625	);}
void	openbuy626	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy624	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy626	=	buy625	    - Close[0]/100*steplevel1;GlobalVariableSet(			test626	,	buy626	);}
void	openbuy627	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy625	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy627	=	buy626	    - Close[0]/100*steplevel1;GlobalVariableSet(			test627	,	buy627	);}
void	openbuy628	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy626	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy628	=	buy627	    - Close[0]/100*steplevel1;GlobalVariableSet(			test628	,	buy628	);}
void	openbuy629	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy627	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy629	=	buy628	    - Close[0]/100*steplevel1;GlobalVariableSet(			test629	,	buy629	);}
void	openbuy630	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy628	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy630	=	buy629	    - Close[0]/100*steplevel1;GlobalVariableSet(			test630	,	buy630	);}
void	openbuy631	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy629	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy631	=	buy630	    - Close[0]/100*steplevel1;GlobalVariableSet(			test631	,	buy631	);}
void	openbuy632	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy630	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy632	=	buy631	    - Close[0]/100*steplevel1;GlobalVariableSet(			test632	,	buy632	);}
void	openbuy633	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy631	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy633	=	buy632	    - Close[0]/100*steplevel1;GlobalVariableSet(			test633	,	buy633	);}
void	openbuy634	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy632	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy634	=	buy633	    - Close[0]/100*steplevel1;GlobalVariableSet(			test634	,	buy634	);}
void	openbuy635	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy633	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy635	=	buy634	    - Close[0]/100*steplevel1;GlobalVariableSet(			test635	,	buy635	);}
void	openbuy636	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy634	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy636	=	buy635	    - Close[0]/100*steplevel1;GlobalVariableSet(			test636	,	buy636	);}
void	openbuy637	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy635	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy637	=	buy636	    - Close[0]/100*steplevel1;GlobalVariableSet(			test637	,	buy637	);}
void	openbuy638	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy636	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy638	=	buy637	    - Close[0]/100*steplevel1;GlobalVariableSet(			test638	,	buy638	);}
void	openbuy639	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy637	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy639	=	buy638	    - Close[0]/100*steplevel1;GlobalVariableSet(			test639	,	buy639	);}
void	openbuy640	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy638	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy640	=	buy639	    - Close[0]/100*steplevel1;GlobalVariableSet(			test640	,	buy640	);}
void	openbuy641	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy639	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy641	=	buy640	    - Close[0]/100*steplevel1;GlobalVariableSet(			test641	,	buy641	);}
void	openbuy642	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy640	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy642	=	buy641	    - Close[0]/100*steplevel1;GlobalVariableSet(			test642	,	buy642	);}
void	openbuy643	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy641	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy643	=	buy642	    - Close[0]/100*steplevel1;GlobalVariableSet(			test643	,	buy643	);}
void	openbuy644	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy642	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy644	=	buy643	    - Close[0]/100*steplevel1;GlobalVariableSet(			test644	,	buy644	);}
void	openbuy645	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy643	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy645	=	buy644	    - Close[0]/100*steplevel1;GlobalVariableSet(			test645	,	buy645	);}
void	openbuy646	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy644	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy646	=	buy645	    - Close[0]/100*steplevel1;GlobalVariableSet(			test646	,	buy646	);}
void	openbuy647	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy645	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy647	=	buy646	    - Close[0]/100*steplevel1;GlobalVariableSet(			test647	,	buy647	);}
void	openbuy648	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy646	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy648	=	buy647	    - Close[0]/100*steplevel1;GlobalVariableSet(			test648	,	buy648	);}
void	openbuy649	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy647	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy649	=	buy648	    - Close[0]/100*steplevel1;GlobalVariableSet(			test649	,	buy649	);}
void	openbuy650	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy648	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy650	=	buy649	    - Close[0]/100*steplevel1;GlobalVariableSet(			test650	,	buy650	);}
void	openbuy651	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy649	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy651	=	buy650	    - Close[0]/100*steplevel1;GlobalVariableSet(			test651	,	buy651	);}
void	openbuy652	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy650	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy652	=	buy651	    - Close[0]/100*steplevel1;GlobalVariableSet(			test652	,	buy652	);}
void	openbuy653	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy651	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy653	=	buy652	    - Close[0]/100*steplevel1;GlobalVariableSet(			test653	,	buy653	);}
void	openbuy654	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy652	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy654	=	buy653	    - Close[0]/100*steplevel1;GlobalVariableSet(			test654	,	buy654	);}
void	openbuy655	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy653	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy655	=	buy654	    - Close[0]/100*steplevel1;GlobalVariableSet(			test655	,	buy655	);}
void	openbuy656	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy654	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy656	=	buy655	    - Close[0]/100*steplevel1;GlobalVariableSet(			test656	,	buy656	);}
void	openbuy657	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy655	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy657	=	buy656	    - Close[0]/100*steplevel1;GlobalVariableSet(			test657	,	buy657	);}
void	openbuy658	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy656	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy658	=	buy657	    - Close[0]/100*steplevel1;GlobalVariableSet(			test658	,	buy658	);}
void	openbuy659	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy657	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy659	=	buy658	    - Close[0]/100*steplevel1;GlobalVariableSet(			test659	,	buy659	);}
void	openbuy660	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy658	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy660	=	buy659	    - Close[0]/100*steplevel1;GlobalVariableSet(			test660	,	buy660	);}
void	openbuy661	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy659	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy661	=	buy660	    - Close[0]/100*steplevel1;GlobalVariableSet(			test661	,	buy661	);}
void	openbuy662	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy660	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy662	=	buy661	    - Close[0]/100*steplevel1;GlobalVariableSet(			test662	,	buy662	);}
void	openbuy663	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy661	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy663	=	buy662	    - Close[0]/100*steplevel1;GlobalVariableSet(			test663	,	buy663	);}
void	openbuy664	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy662	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy664	=	buy663	    - Close[0]/100*steplevel1;GlobalVariableSet(			test664	,	buy664	);}
void	openbuy665	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy663	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy665	=	buy664	    - Close[0]/100*steplevel1;GlobalVariableSet(			test665	,	buy665	);}
void	openbuy666	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy664	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy666	=	buy665	    - Close[0]/100*steplevel1;GlobalVariableSet(			test666	,	buy666	);}
void	openbuy667	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy665	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy667	=	buy666	    - Close[0]/100*steplevel1;GlobalVariableSet(			test667	,	buy667	);}
void	openbuy668	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy666	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy668	=	buy667	    - Close[0]/100*steplevel1;GlobalVariableSet(			test668	,	buy668	);}
void	openbuy669	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy667	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy669	=	buy668	    - Close[0]/100*steplevel1;GlobalVariableSet(			test669	,	buy669	);}
void	openbuy670	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy668	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy670	=	buy669	    - Close[0]/100*steplevel1;GlobalVariableSet(			test670	,	buy670	);}
void	openbuy671	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy669	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy671	=	buy670	    - Close[0]/100*steplevel1;GlobalVariableSet(			test671	,	buy671	);}
void	openbuy672	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy670	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy672	=	buy671	    - Close[0]/100*steplevel1;GlobalVariableSet(			test672	,	buy672	);}
void	openbuy673	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy671	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy673	=	buy672	    - Close[0]/100*steplevel1;GlobalVariableSet(			test673	,	buy673	);}
void	openbuy674	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy672	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy674	=	buy673	    - Close[0]/100*steplevel1;GlobalVariableSet(			test674	,	buy674	);}
void	openbuy675	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy673	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy675	=	buy674	    - Close[0]/100*steplevel1;GlobalVariableSet(			test675	,	buy675	);}
void	openbuy676	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy674	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy676	=	buy675	    - Close[0]/100*steplevel1;GlobalVariableSet(			test676	,	buy676	);}
void	openbuy677	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy675	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy677	=	buy676	    - Close[0]/100*steplevel1;GlobalVariableSet(			test677	,	buy677	);}
void	openbuy678	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy676	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy678	=	buy677	    - Close[0]/100*steplevel1;GlobalVariableSet(			test678	,	buy678	);}
void	openbuy679	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy677	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy679	=	buy678	    - Close[0]/100*steplevel1;GlobalVariableSet(			test679	,	buy679	);}
void	openbuy680	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy678	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy680	=	buy679	    - Close[0]/100*steplevel1;GlobalVariableSet(			test680	,	buy680	);}
void	openbuy681	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy679	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy681	=	buy680	    - Close[0]/100*steplevel1;GlobalVariableSet(			test681	,	buy681	);}
void	openbuy682	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy680	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy682	=	buy681	    - Close[0]/100*steplevel1;GlobalVariableSet(			test682	,	buy682	);}
void	openbuy683	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy681	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy683	=	buy682	    - Close[0]/100*steplevel1;GlobalVariableSet(			test683	,	buy683	);}
void	openbuy684	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy682	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy684	=	buy683	    - Close[0]/100*steplevel1;GlobalVariableSet(			test684	,	buy684	);}
void	openbuy685	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy683	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy685	=	buy684	    - Close[0]/100*steplevel1;GlobalVariableSet(			test685	,	buy685	);}
void	openbuy686	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy684	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy686	=	buy685	    - Close[0]/100*steplevel1;GlobalVariableSet(			test686	,	buy686	);}
void	openbuy687	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy685	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy687	=	buy686	    - Close[0]/100*steplevel1;GlobalVariableSet(			test687	,	buy687	);}
void	openbuy688	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy686	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy688	=	buy687	    - Close[0]/100*steplevel1;GlobalVariableSet(			test688	,	buy688	);}
void	openbuy689	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy687	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy689	=	buy688	    - Close[0]/100*steplevel1;GlobalVariableSet(			test689	,	buy689	);}
void	openbuy690	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy688	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy690	=	buy689	    - Close[0]/100*steplevel1;GlobalVariableSet(			test690	,	buy690	);}
void	openbuy691	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy689	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy691	=	buy690	    - Close[0]/100*steplevel1;GlobalVariableSet(			test691	,	buy691	);}
void	openbuy692	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy690	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy692	=	buy691	    - Close[0]/100*steplevel1;GlobalVariableSet(			test692	,	buy692	);}
void	openbuy693	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy691	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy693	=	buy692	    - Close[0]/100*steplevel1;GlobalVariableSet(			test693	,	buy693	);}
void	openbuy694	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy692	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy694	=	buy693	    - Close[0]/100*steplevel1;GlobalVariableSet(			test694	,	buy694	);}
void	openbuy695	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy693	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy695	=	buy694	    - Close[0]/100*steplevel1;GlobalVariableSet(			test695	,	buy695	);}
void	openbuy696	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy694	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy696	=	buy695	    - Close[0]/100*steplevel1;GlobalVariableSet(			test696	,	buy696	);}
void	openbuy697	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy695	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy697	=	buy696	    - Close[0]/100*steplevel1;GlobalVariableSet(			test697	,	buy697	);}
void	openbuy698	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy696	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy698	=	buy697	    - Close[0]/100*steplevel1;GlobalVariableSet(			test698	,	buy698	);}
void	openbuy699	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy697	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy699	=	buy698	    - Close[0]/100*steplevel1;GlobalVariableSet(			test699	,	buy699	);}
void	openbuy700	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy698	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy700	=	buy699	    - Close[0]/100*steplevel1;GlobalVariableSet(			test700	,	buy700	);}
void	openbuy701	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy699	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy701	=	buy700	    - Close[0]/100*steplevel1;GlobalVariableSet(			test701	,	buy701	);}
void	openbuy702	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy700	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy702	=	buy701	    - Close[0]/100*steplevel1;GlobalVariableSet(			test702	,	buy702	);}
void	openbuy703	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy701	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy703	=	buy702	    - Close[0]/100*steplevel1;GlobalVariableSet(			test703	,	buy703	);}
void	openbuy704	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy702	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy704	=	buy703	    - Close[0]/100*steplevel1;GlobalVariableSet(			test704	,	buy704	);}
void	openbuy705	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy703	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy705	=	buy704	    - Close[0]/100*steplevel1;GlobalVariableSet(			test705	,	buy705	);}
void	openbuy706	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy704	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy706	=	buy705	    - Close[0]/100*steplevel1;GlobalVariableSet(			test706	,	buy706	);}
void	openbuy707	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy705	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy707	=	buy706	    - Close[0]/100*steplevel1;GlobalVariableSet(			test707	,	buy707	);}
void	openbuy708	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy706	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy708	=	buy707	    - Close[0]/100*steplevel1;GlobalVariableSet(			test708	,	buy708	);}
void	openbuy709	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy707	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy709	=	buy708	    - Close[0]/100*steplevel1;GlobalVariableSet(			test709	,	buy709	);}
void	openbuy710	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy708	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy710	=	buy709	    - Close[0]/100*steplevel1;GlobalVariableSet(			test710	,	buy710	);}
void	openbuy711	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy709	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy711	=	buy710	    - Close[0]/100*steplevel1;GlobalVariableSet(			test711	,	buy711	);}
void	openbuy712	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy710	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy712	=	buy711	    - Close[0]/100*steplevel1;GlobalVariableSet(			test712	,	buy712	);}
void	openbuy713	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy711	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy713	=	buy712	    - Close[0]/100*steplevel1;GlobalVariableSet(			test713	,	buy713	);}
void	openbuy714	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy712	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy714	=	buy713	    - Close[0]/100*steplevel1;GlobalVariableSet(			test714	,	buy714	);}
void	openbuy715	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy713	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy715	=	buy714	    - Close[0]/100*steplevel1;GlobalVariableSet(			test715	,	buy715	);}
void	openbuy716	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy714	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy716	=	buy715	    - Close[0]/100*steplevel1;GlobalVariableSet(			test716	,	buy716	);}
void	openbuy717	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy715	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy717	=	buy716	    - Close[0]/100*steplevel1;GlobalVariableSet(			test717	,	buy717	);}
void	openbuy718	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy716	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy718	=	buy717	    - Close[0]/100*steplevel1;GlobalVariableSet(			test718	,	buy718	);}
void	openbuy719	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy717	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy719	=	buy718	    - Close[0]/100*steplevel1;GlobalVariableSet(			test719	,	buy719	);}
void	openbuy720	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy718	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy720	=	buy719	    - Close[0]/100*steplevel1;GlobalVariableSet(			test720	,	buy720	);}
void	openbuy721	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy719	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy721	=	buy720	    - Close[0]/100*steplevel1;GlobalVariableSet(			test721	,	buy721	);}
void	openbuy722	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy720	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy722	=	buy721	    - Close[0]/100*steplevel1;GlobalVariableSet(			test722	,	buy722	);}
void	openbuy723	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy721	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy723	=	buy722	    - Close[0]/100*steplevel1;GlobalVariableSet(			test723	,	buy723	);}
void	openbuy724	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy722	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy724	=	buy723	    - Close[0]/100*steplevel1;GlobalVariableSet(			test724	,	buy724	);}
void	openbuy725	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy723	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy725	=	buy724	    - Close[0]/100*steplevel1;GlobalVariableSet(			test725	,	buy725	);}
void	openbuy726	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy724	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy726	=	buy725	    - Close[0]/100*steplevel1;GlobalVariableSet(			test726	,	buy726	);}
void	openbuy727	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy725	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy727	=	buy726	    - Close[0]/100*steplevel1;GlobalVariableSet(			test727	,	buy727	);}
void	openbuy728	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy726	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy728	=	buy727	    - Close[0]/100*steplevel1;GlobalVariableSet(			test728	,	buy728	);}
void	openbuy729	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy727	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy729	=	buy728	    - Close[0]/100*steplevel1;GlobalVariableSet(			test729	,	buy729	);}
void	openbuy730	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy728	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy730	=	buy729	    - Close[0]/100*steplevel1;GlobalVariableSet(			test730	,	buy730	);}
void	openbuy731	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy729	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy731	=	buy730	    - Close[0]/100*steplevel1;GlobalVariableSet(			test731	,	buy731	);}
void	openbuy732	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy730	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy732	=	buy731	    - Close[0]/100*steplevel1;GlobalVariableSet(			test732	,	buy732	);}
void	openbuy733	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy731	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy733	=	buy732	    - Close[0]/100*steplevel1;GlobalVariableSet(			test733	,	buy733	);}
void	openbuy734	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy732	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy734	=	buy733	    - Close[0]/100*steplevel1;GlobalVariableSet(			test734	,	buy734	);}
void	openbuy735	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy733	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy735	=	buy734	    - Close[0]/100*steplevel1;GlobalVariableSet(			test735	,	buy735	);}
void	openbuy736	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy734	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy736	=	buy735	    - Close[0]/100*steplevel1;GlobalVariableSet(			test736	,	buy736	);}
void	openbuy737	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy735	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy737	=	buy736	    - Close[0]/100*steplevel1;GlobalVariableSet(			test737	,	buy737	);}
void	openbuy738	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy736	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy738	=	buy737	    - Close[0]/100*steplevel1;GlobalVariableSet(			test738	,	buy738	);}
void	openbuy739	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy737	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy739	=	buy738	    - Close[0]/100*steplevel1;GlobalVariableSet(			test739	,	buy739	);}
void	openbuy740	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy738	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy740	=	buy739	    - Close[0]/100*steplevel1;GlobalVariableSet(			test740	,	buy740	);}
void	openbuy741	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy739	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy741	=	buy740	    - Close[0]/100*steplevel1;GlobalVariableSet(			test741	,	buy741	);}
void	openbuy742	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy740	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy742	=	buy741	    - Close[0]/100*steplevel1;GlobalVariableSet(			test742	,	buy742	);}
void	openbuy743	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy741	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy743	=	buy742	    - Close[0]/100*steplevel1;GlobalVariableSet(			test743	,	buy743	);}
void	openbuy744	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy742	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy744	=	buy743	    - Close[0]/100*steplevel1;GlobalVariableSet(			test744	,	buy744	);}
void	openbuy745	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy743	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy745	=	buy744	    - Close[0]/100*steplevel1;GlobalVariableSet(			test745	,	buy745	);}
void	openbuy746	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy744	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy746	=	buy745	    - Close[0]/100*steplevel1;GlobalVariableSet(			test746	,	buy746	);}
void	openbuy747	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy745	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy747	=	buy746	    - Close[0]/100*steplevel1;GlobalVariableSet(			test747	,	buy747	);}
void	openbuy748	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy746	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy748	=	buy747	    - Close[0]/100*steplevel1;GlobalVariableSet(			test748	,	buy748	);}
void	openbuy749	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy747	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy749	=	buy748	    - Close[0]/100*steplevel1;GlobalVariableSet(			test749	,	buy749	);}
void	openbuy750	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy748	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy750	=	buy749	    - Close[0]/100*steplevel1;GlobalVariableSet(			test750	,	buy750	);}
void	openbuy751	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy749	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy751	=	buy750	    - Close[0]/100*steplevel1;GlobalVariableSet(			test751	,	buy751	);}
void	openbuy752	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy750	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy752	=	buy751	    - Close[0]/100*steplevel1;GlobalVariableSet(			test752	,	buy752	);}
void	openbuy753	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy751	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy753	=	buy752	    - Close[0]/100*steplevel1;GlobalVariableSet(			test753	,	buy753	);}
void	openbuy754	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy752	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy754	=	buy753	    - Close[0]/100*steplevel1;GlobalVariableSet(			test754	,	buy754	);}
void	openbuy755	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy753	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy755	=	buy754	    - Close[0]/100*steplevel1;GlobalVariableSet(			test755	,	buy755	);}
void	openbuy756	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy754	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy756	=	buy755	    - Close[0]/100*steplevel1;GlobalVariableSet(			test756	,	buy756	);}
void	openbuy757	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy755	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy757	=	buy756	    - Close[0]/100*steplevel1;GlobalVariableSet(			test757	,	buy757	);}
void	openbuy758	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy756	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy758	=	buy757	    - Close[0]/100*steplevel1;GlobalVariableSet(			test758	,	buy758	);}
void	openbuy759	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy757	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy759	=	buy758	    - Close[0]/100*steplevel1;GlobalVariableSet(			test759	,	buy759	);}
void	openbuy760	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy758	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy760	=	buy759	    - Close[0]/100*steplevel1;GlobalVariableSet(			test760	,	buy760	);}
void	openbuy761	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy759	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy761	=	buy760	    - Close[0]/100*steplevel1;GlobalVariableSet(			test761	,	buy761	);}
void	openbuy762	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy760	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy762	=	buy761	    - Close[0]/100*steplevel1;GlobalVariableSet(			test762	,	buy762	);}
void	openbuy763	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy761	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy763	=	buy762	    - Close[0]/100*steplevel1;GlobalVariableSet(			test763	,	buy763	);}
void	openbuy764	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy762	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy764	=	buy763	    - Close[0]/100*steplevel1;GlobalVariableSet(			test764	,	buy764	);}
void	openbuy765	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy763	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy765	=	buy764	    - Close[0]/100*steplevel1;GlobalVariableSet(			test765	,	buy765	);}
void	openbuy766	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy764	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy766	=	buy765	    - Close[0]/100*steplevel1;GlobalVariableSet(			test766	,	buy766	);}
void	openbuy767	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy765	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy767	=	buy766	    - Close[0]/100*steplevel1;GlobalVariableSet(			test767	,	buy767	);}
void	openbuy768	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy766	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy768	=	buy767	    - Close[0]/100*steplevel1;GlobalVariableSet(			test768	,	buy768	);}
void	openbuy769	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy767	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy769	=	buy768	    - Close[0]/100*steplevel1;GlobalVariableSet(			test769	,	buy769	);}
void	openbuy770	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy768	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy770	=	buy769	    - Close[0]/100*steplevel1;GlobalVariableSet(			test770	,	buy770	);}
void	openbuy771	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy769	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy771	=	buy770	    - Close[0]/100*steplevel1;GlobalVariableSet(			test771	,	buy771	);}
void	openbuy772	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy770	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy772	=	buy771	    - Close[0]/100*steplevel1;GlobalVariableSet(			test772	,	buy772	);}
void	openbuy773	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy771	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy773	=	buy772	    - Close[0]/100*steplevel1;GlobalVariableSet(			test773	,	buy773	);}
void	openbuy774	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy772	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy774	=	buy773	    - Close[0]/100*steplevel1;GlobalVariableSet(			test774	,	buy774	);}
void	openbuy775	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy773	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy775	=	buy774	    - Close[0]/100*steplevel1;GlobalVariableSet(			test775	,	buy775	);}
void	openbuy776	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy774	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy776	=	buy775	    - Close[0]/100*steplevel1;GlobalVariableSet(			test776	,	buy776	);}
void	openbuy777	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy775	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy777	=	buy776	    - Close[0]/100*steplevel1;GlobalVariableSet(			test777	,	buy777	);}
void	openbuy778	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy776	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy778	=	buy777	    - Close[0]/100*steplevel1;GlobalVariableSet(			test778	,	buy778	);}
void	openbuy779	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy777	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy779	=	buy778	    - Close[0]/100*steplevel1;GlobalVariableSet(			test779	,	buy779	);}
void	openbuy780	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy778	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy780	=	buy779	    - Close[0]/100*steplevel1;GlobalVariableSet(			test780	,	buy780	);}
void	openbuy781	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy779	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy781	=	buy780	    - Close[0]/100*steplevel1;GlobalVariableSet(			test781	,	buy781	);}
void	openbuy782	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy780	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy782	=	buy781	    - Close[0]/100*steplevel1;GlobalVariableSet(			test782	,	buy782	);}
void	openbuy783	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy781	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy783	=	buy782	    - Close[0]/100*steplevel1;GlobalVariableSet(			test783	,	buy783	);}
void	openbuy784	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy782	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy784	=	buy783	    - Close[0]/100*steplevel1;GlobalVariableSet(			test784	,	buy784	);}
void	openbuy785	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy783	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy785	=	buy784	    - Close[0]/100*steplevel1;GlobalVariableSet(			test785	,	buy785	);}
void	openbuy786	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy784	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy786	=	buy785	    - Close[0]/100*steplevel1;GlobalVariableSet(			test786	,	buy786	);}
void	openbuy787	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy785	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy787	=	buy786	    - Close[0]/100*steplevel1;GlobalVariableSet(			test787	,	buy787	);}
void	openbuy788	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy786	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy788	=	buy787	    - Close[0]/100*steplevel1;GlobalVariableSet(			test788	,	buy788	);}
void	openbuy789	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy787	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy789	=	buy788	    - Close[0]/100*steplevel1;GlobalVariableSet(			test789	,	buy789	);}
void	openbuy790	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy788	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy790	=	buy789	    - Close[0]/100*steplevel1;GlobalVariableSet(			test790	,	buy790	);}
void	openbuy791	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy789	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy791	=	buy790	    - Close[0]/100*steplevel1;GlobalVariableSet(			test791	,	buy791	);}
void	openbuy792	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy790	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy792	=	buy791	    - Close[0]/100*steplevel1;GlobalVariableSet(			test792	,	buy792	);}
void	openbuy793	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy791	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy793	=	buy792	    - Close[0]/100*steplevel1;GlobalVariableSet(			test793	,	buy793	);}
void	openbuy794	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy792	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy794	=	buy793	    - Close[0]/100*steplevel1;GlobalVariableSet(			test794	,	buy794	);}
void	openbuy795	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy793	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy795	=	buy794	    - Close[0]/100*steplevel1;GlobalVariableSet(			test795	,	buy795	);}
void	openbuy796	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy794	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy796	=	buy795	    - Close[0]/100*steplevel1;GlobalVariableSet(			test796	,	buy796	);}
void	openbuy797	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy795	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy797	=	buy796	    - Close[0]/100*steplevel1;GlobalVariableSet(			test797	,	buy797	);}
void	openbuy798	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy796	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy798	=	buy797	    - Close[0]/100*steplevel1;GlobalVariableSet(			test798	,	buy798	);}
void	openbuy799	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy797	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy799	=	buy798	    - Close[0]/100*steplevel1;GlobalVariableSet(			test799	,	buy799	);}
void	openbuy800	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy798	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy800	=	buy799	    - Close[0]/100*steplevel1;GlobalVariableSet(			test800	,	buy800	);}
void	openbuy801	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy799	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy801	=	buy800	    - Close[0]/100*steplevel1;GlobalVariableSet(			test801	,	buy801	);}
void	openbuy802	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy800	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy802	=	buy801	    - Close[0]/100*steplevel1;GlobalVariableSet(			test802	,	buy802	);}
void	openbuy803	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy801	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy803	=	buy802	    - Close[0]/100*steplevel1;GlobalVariableSet(			test803	,	buy803	);}
void	openbuy804	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy802	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy804	=	buy803	    - Close[0]/100*steplevel1;GlobalVariableSet(			test804	,	buy804	);}
void	openbuy805	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy803	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy805	=	buy804	    - Close[0]/100*steplevel1;GlobalVariableSet(			test805	,	buy805	);}
void	openbuy806	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy804	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy806	=	buy805	    - Close[0]/100*steplevel1;GlobalVariableSet(			test806	,	buy806	);}
void	openbuy807	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy805	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy807	=	buy806	    - Close[0]/100*steplevel1;GlobalVariableSet(			test807	,	buy807	);}
void	openbuy808	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy806	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy808	=	buy807	    - Close[0]/100*steplevel1;GlobalVariableSet(			test808	,	buy808	);}
void	openbuy809	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy807	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy809	=	buy808	    - Close[0]/100*steplevel1;GlobalVariableSet(			test809	,	buy809	);}
void	openbuy810	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy808	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy810	=	buy809	    - Close[0]/100*steplevel1;GlobalVariableSet(			test810	,	buy810	);}
void	openbuy811	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy809	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy811	=	buy810	    - Close[0]/100*steplevel1;GlobalVariableSet(			test811	,	buy811	);}
void	openbuy812	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy810	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy812	=	buy811	    - Close[0]/100*steplevel1;GlobalVariableSet(			test812	,	buy812	);}
void	openbuy813	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy811	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy813	=	buy812	    - Close[0]/100*steplevel1;GlobalVariableSet(			test813	,	buy813	);}
void	openbuy814	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy812	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy814	=	buy813	    - Close[0]/100*steplevel1;GlobalVariableSet(			test814	,	buy814	);}
void	openbuy815	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy813	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy815	=	buy814	    - Close[0]/100*steplevel1;GlobalVariableSet(			test815	,	buy815	);}
void	openbuy816	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy814	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy816	=	buy815	    - Close[0]/100*steplevel1;GlobalVariableSet(			test816	,	buy816	);}
void	openbuy817	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy815	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy817	=	buy816	    - Close[0]/100*steplevel1;GlobalVariableSet(			test817	,	buy817	);}
void	openbuy818	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy816	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy818	=	buy817	    - Close[0]/100*steplevel1;GlobalVariableSet(			test818	,	buy818	);}
void	openbuy819	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy817	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy819	=	buy818	    - Close[0]/100*steplevel1;GlobalVariableSet(			test819	,	buy819	);}
void	openbuy820	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy818	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy820	=	buy819	    - Close[0]/100*steplevel1;GlobalVariableSet(			test820	,	buy820	);}
void	openbuy821	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy819	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy821	=	buy820	    - Close[0]/100*steplevel1;GlobalVariableSet(			test821	,	buy821	);}
void	openbuy822	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy820	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy822	=	buy821	    - Close[0]/100*steplevel1;GlobalVariableSet(			test822	,	buy822	);}
void	openbuy823	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy821	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy823	=	buy822	    - Close[0]/100*steplevel1;GlobalVariableSet(			test823	,	buy823	);}
void	openbuy824	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy822	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy824	=	buy823	    - Close[0]/100*steplevel1;GlobalVariableSet(			test824	,	buy824	);}
void	openbuy825	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy823	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy825	=	buy824	    - Close[0]/100*steplevel1;GlobalVariableSet(			test825	,	buy825	);}
void	openbuy826	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy824	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy826	=	buy825	    - Close[0]/100*steplevel1;GlobalVariableSet(			test826	,	buy826	);}
void	openbuy827	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy825	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy827	=	buy826	    - Close[0]/100*steplevel1;GlobalVariableSet(			test827	,	buy827	);}
void	openbuy828	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy826	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy828	=	buy827	    - Close[0]/100*steplevel1;GlobalVariableSet(			test828	,	buy828	);}
void	openbuy829	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy827	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy829	=	buy828	    - Close[0]/100*steplevel1;GlobalVariableSet(			test829	,	buy829	);}
void	openbuy830	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy828	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy830	=	buy829	    - Close[0]/100*steplevel1;GlobalVariableSet(			test830	,	buy830	);}
void	openbuy831	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy829	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy831	=	buy830	    - Close[0]/100*steplevel1;GlobalVariableSet(			test831	,	buy831	);}
void	openbuy832	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy830	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy832	=	buy831	    - Close[0]/100*steplevel1;GlobalVariableSet(			test832	,	buy832	);}
void	openbuy833	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy831	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy833	=	buy832	    - Close[0]/100*steplevel1;GlobalVariableSet(			test833	,	buy833	);}
void	openbuy834	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy832	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy834	=	buy833	    - Close[0]/100*steplevel1;GlobalVariableSet(			test834	,	buy834	);}
void	openbuy835	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy833	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy835	=	buy834	    - Close[0]/100*steplevel1;GlobalVariableSet(			test835	,	buy835	);}
void	openbuy836	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy834	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy836	=	buy835	    - Close[0]/100*steplevel1;GlobalVariableSet(			test836	,	buy836	);}
void	openbuy837	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy835	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy837	=	buy836	    - Close[0]/100*steplevel1;GlobalVariableSet(			test837	,	buy837	);}
void	openbuy838	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy836	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy838	=	buy837	    - Close[0]/100*steplevel1;GlobalVariableSet(			test838	,	buy838	);}
void	openbuy839	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy837	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy839	=	buy838	    - Close[0]/100*steplevel1;GlobalVariableSet(			test839	,	buy839	);}
void	openbuy840	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy838	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy840	=	buy839	    - Close[0]/100*steplevel1;GlobalVariableSet(			test840	,	buy840	);}
void	openbuy841	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy839	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy841	=	buy840	    - Close[0]/100*steplevel1;GlobalVariableSet(			test841	,	buy841	);}
void	openbuy842	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy840	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy842	=	buy841	    - Close[0]/100*steplevel1;GlobalVariableSet(			test842	,	buy842	);}
void	openbuy843	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy841	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy843	=	buy842	    - Close[0]/100*steplevel1;GlobalVariableSet(			test843	,	buy843	);}
void	openbuy844	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy842	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy844	=	buy843	    - Close[0]/100*steplevel1;GlobalVariableSet(			test844	,	buy844	);}
void	openbuy845	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy843	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy845	=	buy844	    - Close[0]/100*steplevel1;GlobalVariableSet(			test845	,	buy845	);}
void	openbuy846	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy844	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy846	=	buy845	    - Close[0]/100*steplevel1;GlobalVariableSet(			test846	,	buy846	);}
void	openbuy847	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy845	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy847	=	buy846	    - Close[0]/100*steplevel1;GlobalVariableSet(			test847	,	buy847	);}
void	openbuy848	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy846	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy848	=	buy847	    - Close[0]/100*steplevel1;GlobalVariableSet(			test848	,	buy848	);}
void	openbuy849	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy847	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy849	=	buy848	    - Close[0]/100*steplevel1;GlobalVariableSet(			test849	,	buy849	);}
void	openbuy850	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy848	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy850	=	buy849	    - Close[0]/100*steplevel1;GlobalVariableSet(			test850	,	buy850	);}
void	openbuy851	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy849	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy851	=	buy850	    - Close[0]/100*steplevel1;GlobalVariableSet(			test851	,	buy851	);}
void	openbuy852	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy850	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy852	=	buy851	    - Close[0]/100*steplevel1;GlobalVariableSet(			test852	,	buy852	);}
void	openbuy853	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy851	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy853	=	buy852	    - Close[0]/100*steplevel1;GlobalVariableSet(			test853	,	buy853	);}
void	openbuy854	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy852	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy854	=	buy853	    - Close[0]/100*steplevel1;GlobalVariableSet(			test854	,	buy854	);}
void	openbuy855	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy853	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy855	=	buy854	    - Close[0]/100*steplevel1;GlobalVariableSet(			test855	,	buy855	);}
void	openbuy856	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy854	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy856	=	buy855	    - Close[0]/100*steplevel1;GlobalVariableSet(			test856	,	buy856	);}
void	openbuy857	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy855	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy857	=	buy856	    - Close[0]/100*steplevel1;GlobalVariableSet(			test857	,	buy857	);}
void	openbuy858	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy856	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy858	=	buy857	    - Close[0]/100*steplevel1;GlobalVariableSet(			test858	,	buy858	);}
void	openbuy859	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy857	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy859	=	buy858	    - Close[0]/100*steplevel1;GlobalVariableSet(			test859	,	buy859	);}
void	openbuy860	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy858	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy860	=	buy859	    - Close[0]/100*steplevel1;GlobalVariableSet(			test860	,	buy860	);}
void	openbuy861	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy859	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy861	=	buy860	    - Close[0]/100*steplevel1;GlobalVariableSet(			test861	,	buy861	);}
void	openbuy862	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy860	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy862	=	buy861	    - Close[0]/100*steplevel1;GlobalVariableSet(			test862	,	buy862	);}
void	openbuy863	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy861	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy863	=	buy862	    - Close[0]/100*steplevel1;GlobalVariableSet(			test863	,	buy863	);}
void	openbuy864	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy862	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy864	=	buy863	    - Close[0]/100*steplevel1;GlobalVariableSet(			test864	,	buy864	);}
void	openbuy865	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy863	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy865	=	buy864	    - Close[0]/100*steplevel1;GlobalVariableSet(			test865	,	buy865	);}
void	openbuy866	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy864	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy866	=	buy865	    - Close[0]/100*steplevel1;GlobalVariableSet(			test866	,	buy866	);}
void	openbuy867	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy865	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy867	=	buy866	    - Close[0]/100*steplevel1;GlobalVariableSet(			test867	,	buy867	);}
void	openbuy868	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy866	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy868	=	buy867	    - Close[0]/100*steplevel1;GlobalVariableSet(			test868	,	buy868	);}
void	openbuy869	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy867	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy869	=	buy868	    - Close[0]/100*steplevel1;GlobalVariableSet(			test869	,	buy869	);}
void	openbuy870	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy868	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy870	=	buy869	    - Close[0]/100*steplevel1;GlobalVariableSet(			test870	,	buy870	);}
void	openbuy871	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy869	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy871	=	buy870	    - Close[0]/100*steplevel1;GlobalVariableSet(			test871	,	buy871	);}
void	openbuy872	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy870	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy872	=	buy871	    - Close[0]/100*steplevel1;GlobalVariableSet(			test872	,	buy872	);}
void	openbuy873	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy871	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy873	=	buy872	    - Close[0]/100*steplevel1;GlobalVariableSet(			test873	,	buy873	);}
void	openbuy874	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy872	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy874	=	buy873	    - Close[0]/100*steplevel1;GlobalVariableSet(			test874	,	buy874	);}
void	openbuy875	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy873	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy875	=	buy874	    - Close[0]/100*steplevel1;GlobalVariableSet(			test875	,	buy875	);}
void	openbuy876	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy874	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy876	=	buy875	    - Close[0]/100*steplevel1;GlobalVariableSet(			test876	,	buy876	);}
void	openbuy877	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy875	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy877	=	buy876	    - Close[0]/100*steplevel1;GlobalVariableSet(			test877	,	buy877	);}
void	openbuy878	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy876	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy878	=	buy877	    - Close[0]/100*steplevel1;GlobalVariableSet(			test878	,	buy878	);}
void	openbuy879	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy877	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy879	=	buy878	    - Close[0]/100*steplevel1;GlobalVariableSet(			test879	,	buy879	);}
void	openbuy880	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy878	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy880	=	buy879	    - Close[0]/100*steplevel1;GlobalVariableSet(			test880	,	buy880	);}
void	openbuy881	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy879	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy881	=	buy880	    - Close[0]/100*steplevel1;GlobalVariableSet(			test881	,	buy881	);}
void	openbuy882	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy880	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy882	=	buy881	    - Close[0]/100*steplevel1;GlobalVariableSet(			test882	,	buy882	);}
void	openbuy883	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy881	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy883	=	buy882	    - Close[0]/100*steplevel1;GlobalVariableSet(			test883	,	buy883	);}
void	openbuy884	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy882	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy884	=	buy883	    - Close[0]/100*steplevel1;GlobalVariableSet(			test884	,	buy884	);}
void	openbuy885	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy883	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy885	=	buy884	    - Close[0]/100*steplevel1;GlobalVariableSet(			test885	,	buy885	);}
void	openbuy886	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy884	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy886	=	buy885	    - Close[0]/100*steplevel1;GlobalVariableSet(			test886	,	buy886	);}
void	openbuy887	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy885	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy887	=	buy886	    - Close[0]/100*steplevel1;GlobalVariableSet(			test887	,	buy887	);}
void	openbuy888	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy886	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy888	=	buy887	    - Close[0]/100*steplevel1;GlobalVariableSet(			test888	,	buy888	);}
void	openbuy889	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy887	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy889	=	buy888	    - Close[0]/100*steplevel1;GlobalVariableSet(			test889	,	buy889	);}
void	openbuy890	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy888	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy890	=	buy889	    - Close[0]/100*steplevel1;GlobalVariableSet(			test890	,	buy890	);}
void	openbuy891	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy889	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy891	=	buy890	    - Close[0]/100*steplevel1;GlobalVariableSet(			test891	,	buy891	);}
void	openbuy892	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy890	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy892	=	buy891	    - Close[0]/100*steplevel1;GlobalVariableSet(			test892	,	buy892	);}
void	openbuy893	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy891	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy893	=	buy892	    - Close[0]/100*steplevel1;GlobalVariableSet(			test893	,	buy893	);}
void	openbuy894	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy892	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy894	=	buy893	    - Close[0]/100*steplevel1;GlobalVariableSet(			test894	,	buy894	);}
void	openbuy895	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy893	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy895	=	buy894	    - Close[0]/100*steplevel1;GlobalVariableSet(			test895	,	buy895	);}
void	openbuy896	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy894	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy896	=	buy895	    - Close[0]/100*steplevel1;GlobalVariableSet(			test896	,	buy896	);}
void	openbuy897	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy895	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy897	=	buy896	    - Close[0]/100*steplevel1;GlobalVariableSet(			test897	,	buy897	);}
void	openbuy898	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy896	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy898	=	buy897	    - Close[0]/100*steplevel1;GlobalVariableSet(			test898	,	buy898	);}
void	openbuy899	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy897	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy899	=	buy898	    - Close[0]/100*steplevel1;GlobalVariableSet(			test899	,	buy899	);}
void	openbuy900	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy898	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy900	=	buy899	    - Close[0]/100*steplevel1;GlobalVariableSet(			test900	,	buy900	);}
void	openbuy901	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy899	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy901	=	buy900	    - Close[0]/100*steplevel1;GlobalVariableSet(			test901	,	buy901	);}
void	openbuy902	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy900	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy902	=	buy901	    - Close[0]/100*steplevel1;GlobalVariableSet(			test902	,	buy902	);}
void	openbuy903	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy901	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy903	=	buy902	    - Close[0]/100*steplevel1;GlobalVariableSet(			test903	,	buy903	);}
void	openbuy904	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy902	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy904	=	buy903	    - Close[0]/100*steplevel1;GlobalVariableSet(			test904	,	buy904	);}
void	openbuy905	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy903	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy905	=	buy904	    - Close[0]/100*steplevel1;GlobalVariableSet(			test905	,	buy905	);}
void	openbuy906	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy904	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy906	=	buy905	    - Close[0]/100*steplevel1;GlobalVariableSet(			test906	,	buy906	);}
void	openbuy907	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy905	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy907	=	buy906	    - Close[0]/100*steplevel1;GlobalVariableSet(			test907	,	buy907	);}
void	openbuy908	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy906	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy908	=	buy907	    - Close[0]/100*steplevel1;GlobalVariableSet(			test908	,	buy908	);}
void	openbuy909	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy907	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy909	=	buy908	    - Close[0]/100*steplevel1;GlobalVariableSet(			test909	,	buy909	);}
void	openbuy910	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy908	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy910	=	buy909	    - Close[0]/100*steplevel1;GlobalVariableSet(			test910	,	buy910	);}
void	openbuy911	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy909	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy911	=	buy910	    - Close[0]/100*steplevel1;GlobalVariableSet(			test911	,	buy911	);}
void	openbuy912	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy910	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy912	=	buy911	    - Close[0]/100*steplevel1;GlobalVariableSet(			test912	,	buy912	);}
void	openbuy913	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy911	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy913	=	buy912	    - Close[0]/100*steplevel1;GlobalVariableSet(			test913	,	buy913	);}
void	openbuy914	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy912	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy914	=	buy913	    - Close[0]/100*steplevel1;GlobalVariableSet(			test914	,	buy914	);}
void	openbuy915	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy913	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy915	=	buy914	    - Close[0]/100*steplevel1;GlobalVariableSet(			test915	,	buy915	);}
void	openbuy916	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy914	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy916	=	buy915	    - Close[0]/100*steplevel1;GlobalVariableSet(			test916	,	buy916	);}
void	openbuy917	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy915	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy917	=	buy916	    - Close[0]/100*steplevel1;GlobalVariableSet(			test917	,	buy917	);}
void	openbuy918	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy916	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy918	=	buy917	    - Close[0]/100*steplevel1;GlobalVariableSet(			test918	,	buy918	);}
void	openbuy919	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy917	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy919	=	buy918	    - Close[0]/100*steplevel1;GlobalVariableSet(			test919	,	buy919	);}
void	openbuy920	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy918	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy920	=	buy919	    - Close[0]/100*steplevel1;GlobalVariableSet(			test920	,	buy920	);}
void	openbuy921	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy919	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy921	=	buy920	    - Close[0]/100*steplevel1;GlobalVariableSet(			test921	,	buy921	);}
void	openbuy922	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy920	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy922	=	buy921	    - Close[0]/100*steplevel1;GlobalVariableSet(			test922	,	buy922	);}
void	openbuy923	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy921	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy923	=	buy922	    - Close[0]/100*steplevel1;GlobalVariableSet(			test923	,	buy923	);}
void	openbuy924	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy922	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy924	=	buy923	    - Close[0]/100*steplevel1;GlobalVariableSet(			test924	,	buy924	);}
void	openbuy925	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy923	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy925	=	buy924	    - Close[0]/100*steplevel1;GlobalVariableSet(			test925	,	buy925	);}
void	openbuy926	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy924	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy926	=	buy925	    - Close[0]/100*steplevel1;GlobalVariableSet(			test926	,	buy926	);}
void	openbuy927	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy925	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy927	=	buy926	    - Close[0]/100*steplevel1;GlobalVariableSet(			test927	,	buy927	);}
void	openbuy928	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy926	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy928	=	buy927	    - Close[0]/100*steplevel1;GlobalVariableSet(			test928	,	buy928	);}
void	openbuy929	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy927	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy929	=	buy928	    - Close[0]/100*steplevel1;GlobalVariableSet(			test929	,	buy929	);}
void	openbuy930	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy928	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy930	=	buy929	    - Close[0]/100*steplevel1;GlobalVariableSet(			test930	,	buy930	);}
void	openbuy931	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy929	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy931	=	buy930	    - Close[0]/100*steplevel1;GlobalVariableSet(			test931	,	buy931	);}
void	openbuy932	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy930	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy932	=	buy931	    - Close[0]/100*steplevel1;GlobalVariableSet(			test932	,	buy932	);}
void	openbuy933	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy931	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy933	=	buy932	    - Close[0]/100*steplevel1;GlobalVariableSet(			test933	,	buy933	);}
void	openbuy934	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy932	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy934	=	buy933	    - Close[0]/100*steplevel1;GlobalVariableSet(			test934	,	buy934	);}
void	openbuy935	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy933	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy935	=	buy934	    - Close[0]/100*steplevel1;GlobalVariableSet(			test935	,	buy935	);}
void	openbuy936	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy934	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy936	=	buy935	    - Close[0]/100*steplevel1;GlobalVariableSet(			test936	,	buy936	);}
void	openbuy937	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy935	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy937	=	buy936	    - Close[0]/100*steplevel1;GlobalVariableSet(			test937	,	buy937	);}
void	openbuy938	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy936	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy938	=	buy937	    - Close[0]/100*steplevel1;GlobalVariableSet(			test938	,	buy938	);}
void	openbuy939	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy937	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy939	=	buy938	    - Close[0]/100*steplevel1;GlobalVariableSet(			test939	,	buy939	);}
void	openbuy940	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy938	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy940	=	buy939	    - Close[0]/100*steplevel1;GlobalVariableSet(			test940	,	buy940	);}
void	openbuy941	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy939	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy941	=	buy940	    - Close[0]/100*steplevel1;GlobalVariableSet(			test941	,	buy941	);}
void	openbuy942	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy940	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy942	=	buy941	    - Close[0]/100*steplevel1;GlobalVariableSet(			test942	,	buy942	);}
void	openbuy943	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy941	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy943	=	buy942	    - Close[0]/100*steplevel1;GlobalVariableSet(			test943	,	buy943	);}
void	openbuy944	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy942	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy944	=	buy943	    - Close[0]/100*steplevel1;GlobalVariableSet(			test944	,	buy944	);}
void	openbuy945	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy943	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy945	=	buy944	    - Close[0]/100*steplevel1;GlobalVariableSet(			test945	,	buy945	);}
void	openbuy946	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy944	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy946	=	buy945	    - Close[0]/100*steplevel1;GlobalVariableSet(			test946	,	buy946	);}
void	openbuy947	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy945	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy947	=	buy946	    - Close[0]/100*steplevel1;GlobalVariableSet(			test947	,	buy947	);}
void	openbuy948	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy946	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy948	=	buy947	    - Close[0]/100*steplevel1;GlobalVariableSet(			test948	,	buy948	);}
void	openbuy949	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy947	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy949	=	buy948	    - Close[0]/100*steplevel1;GlobalVariableSet(			test949	,	buy949	);}
void	openbuy950	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy948	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy950	=	buy949	    - Close[0]/100*steplevel1;GlobalVariableSet(			test950	,	buy950	);}
void	openbuy951	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy949	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy951	=	buy950	    - Close[0]/100*steplevel1;GlobalVariableSet(			test951	,	buy951	);}
void	openbuy952	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy950	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy952	=	buy951	    - Close[0]/100*steplevel1;GlobalVariableSet(			test952	,	buy952	);}
void	openbuy953	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy951	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy953	=	buy952	    - Close[0]/100*steplevel1;GlobalVariableSet(			test953	,	buy953	);}
void	openbuy954	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy952	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy954	=	buy953	    - Close[0]/100*steplevel1;GlobalVariableSet(			test954	,	buy954	);}
void	openbuy955	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy953	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy955	=	buy954	    - Close[0]/100*steplevel1;GlobalVariableSet(			test955	,	buy955	);}
void	openbuy956	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy954	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy956	=	buy955	    - Close[0]/100*steplevel1;GlobalVariableSet(			test956	,	buy956	);}
void	openbuy957	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy955	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy957	=	buy956	    - Close[0]/100*steplevel1;GlobalVariableSet(			test957	,	buy957	);}
void	openbuy958	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy956	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy958	=	buy957	    - Close[0]/100*steplevel1;GlobalVariableSet(			test958	,	buy958	);}
void	openbuy959	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy957	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy959	=	buy958	    - Close[0]/100*steplevel1;GlobalVariableSet(			test959	,	buy959	);}
void	openbuy960	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy958	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy960	=	buy959	    - Close[0]/100*steplevel1;GlobalVariableSet(			test960	,	buy960	);}
void	openbuy961	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy959	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy961	=	buy960	    - Close[0]/100*steplevel1;GlobalVariableSet(			test961	,	buy961	);}
void	openbuy962	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy960	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy962	=	buy961	    - Close[0]/100*steplevel1;GlobalVariableSet(			test962	,	buy962	);}
void	openbuy963	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy961	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy963	=	buy962	    - Close[0]/100*steplevel1;GlobalVariableSet(			test963	,	buy963	);}
void	openbuy964	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy962	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy964	=	buy963	    - Close[0]/100*steplevel1;GlobalVariableSet(			test964	,	buy964	);}
void	openbuy965	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy963	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy965	=	buy964	    - Close[0]/100*steplevel1;GlobalVariableSet(			test965	,	buy965	);}
void	openbuy966	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy964	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy966	=	buy965	    - Close[0]/100*steplevel1;GlobalVariableSet(			test966	,	buy966	);}
void	openbuy967	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy965	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy967	=	buy966	    - Close[0]/100*steplevel1;GlobalVariableSet(			test967	,	buy967	);}
void	openbuy968	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy966	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy968	=	buy967	    - Close[0]/100*steplevel1;GlobalVariableSet(			test968	,	buy968	);}
void	openbuy969	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy967	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy969	=	buy968	    - Close[0]/100*steplevel1;GlobalVariableSet(			test969	,	buy969	);}
void	openbuy970	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy968	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy970	=	buy969	    - Close[0]/100*steplevel1;GlobalVariableSet(			test970	,	buy970	);}
void	openbuy971	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy969	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy971	=	buy970	    - Close[0]/100*steplevel1;GlobalVariableSet(			test971	,	buy971	);}
void	openbuy972	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy970	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy972	=	buy971	    - Close[0]/100*steplevel1;GlobalVariableSet(			test972	,	buy972	);}
void	openbuy973	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy971	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy973	=	buy972	    - Close[0]/100*steplevel1;GlobalVariableSet(			test973	,	buy973	);}
void	openbuy974	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy972	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy974	=	buy973	    - Close[0]/100*steplevel1;GlobalVariableSet(			test974	,	buy974	);}
void	openbuy975	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy973	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy975	=	buy974	    - Close[0]/100*steplevel1;GlobalVariableSet(			test975	,	buy975	);}
void	openbuy976	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy974	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy976	=	buy975	    - Close[0]/100*steplevel1;GlobalVariableSet(			test976	,	buy976	);}
void	openbuy977	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy975	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy977	=	buy976	    - Close[0]/100*steplevel1;GlobalVariableSet(			test977	,	buy977	);}
void	openbuy978	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy976	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy978	=	buy977	    - Close[0]/100*steplevel1;GlobalVariableSet(			test978	,	buy978	);}
void	openbuy979	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy977	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy979	=	buy978	    - Close[0]/100*steplevel1;GlobalVariableSet(			test979	,	buy979	);}
void	openbuy980	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy978	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy980	=	buy979	    - Close[0]/100*steplevel1;GlobalVariableSet(			test980	,	buy980	);}
void	openbuy981	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy979	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy981	=	buy980	    - Close[0]/100*steplevel1;GlobalVariableSet(			test981	,	buy981	);}
void	openbuy982	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy980	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy982	=	buy981	    - Close[0]/100*steplevel1;GlobalVariableSet(			test982	,	buy982	);}
void	openbuy983	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy981	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy983	=	buy982	    - Close[0]/100*steplevel1;GlobalVariableSet(			test983	,	buy983	);}
void	openbuy984	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy982	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy984	=	buy983	    - Close[0]/100*steplevel1;GlobalVariableSet(			test984	,	buy984	);}
void	openbuy985	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy983	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy985	=	buy984	    - Close[0]/100*steplevel1;GlobalVariableSet(			test985	,	buy985	);}
void	openbuy986	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy984	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy986	=	buy985	    - Close[0]/100*steplevel1;GlobalVariableSet(			test986	,	buy986	);}
void	openbuy987	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy985	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy987	=	buy986	    - Close[0]/100*steplevel1;GlobalVariableSet(			test987	,	buy987	);}
void	openbuy988	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy986	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy988	=	buy987	    - Close[0]/100*steplevel1;GlobalVariableSet(			test988	,	buy988	);}
void	openbuy989	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy987	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy989	=	buy988	    - Close[0]/100*steplevel1;GlobalVariableSet(			test989	,	buy989	);}
void	openbuy990	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy988	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy990	=	buy989	    - Close[0]/100*steplevel1;GlobalVariableSet(			test990	,	buy990	);}
void	openbuy991	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy989	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy991	=	buy990	    - Close[0]/100*steplevel1;GlobalVariableSet(			test991	,	buy991	);}
void	openbuy992	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy990	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy992	=	buy991	    - Close[0]/100*steplevel1;GlobalVariableSet(			test992	,	buy992	);}
void	openbuy993	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy991	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy993	=	buy992	    - Close[0]/100*steplevel1;GlobalVariableSet(			test993	,	buy993	);}
void	openbuy994	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy992	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy994	=	buy993	    - Close[0]/100*steplevel1;GlobalVariableSet(			test994	,	buy994	);}
void	openbuy995	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy993	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy995	=	buy994	    - Close[0]/100*steplevel1;GlobalVariableSet(			test995	,	buy995	);}
void	openbuy996	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy994	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy996	=	buy995	    - Close[0]/100*steplevel1;GlobalVariableSet(			test996	,	buy996	);}
void	openbuy997	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy995	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy997	=	buy996	    - Close[0]/100*steplevel1;GlobalVariableSet(			test997	,	buy997	);}
void	openbuy998	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy996	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy998	=	buy997	    - Close[0]/100*steplevel1;GlobalVariableSet(			test998	,	buy998	);}
void	openbuy999	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy997	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy999	=	buy998	    - Close[0]/100*steplevel1;GlobalVariableSet(			test999	,	buy999	);}
void	openbuy1000	  (){double b = Close[0];double lot = 0.01;double c = Close[1]; double d = iMA(NULL,timechart,maperiod,0,MODE_EMA,PRICE_CLOSE,1);	if(b<	buy998	)if (Close[0]>lowprice)if(b>d)buyorder = OrderSend(Symbol(),OP_BUY,lot,Ask,3,0,0,"Buy",magicnumber,Green);	buy1000	=	buy999	    - Close[0]/100*steplevel1;GlobalVariableSet(			test1000	,	buy1000	);}

void AdjustTrail()
{
for(int b = OrdersTotal()-1;b >= 0;b--)
{
if(OrderSelect(b,SELECT_BY_POS,MODE_TRADES))
if(OrderMagicNumber()==magicnumber)
if (OrderSymbol()==Symbol())
if(OrderType()==OP_BUY)
if(Bid-OrderOpenPrice()>(steplevel1+trailerpoints1+OrderSwap())*pips)
if(OrderStopLoss()<Bid-pips*(trailerpoints1))
adjustorder = OrderModify(OrderTicket(),OrderOpenPrice(),Bid-(pips*(trailerpoints1)),OrderTakeProfit(),0,CLR_NONE);

}}

void CloseProfitBeforeClosingTime()
{
for(int b = OrdersTotal()-1;b >= 0;b--)
{
if(OrderSelect(b,SELECT_BY_POS,MODE_TRADES))
if(OrderMagicNumber()==magicnumber)
if (OrderSymbol()==Symbol())
if(OrderType()==OP_BUY)
if (OrderProfit()>Profit*pips)
if(CloseAll  && Hour() == hour && Minute() >= minute)
 OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_BID), 5, Red );
}}



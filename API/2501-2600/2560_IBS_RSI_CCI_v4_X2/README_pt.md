# Estratégia IBS RSI CCI v4 X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia IBS RSI CCI v4 X2** é um sistema de momentum multi-período que combina o Internal Bar Strength (IBS), o Relative Strength Index (RSI) e o Commodity Channel Index (CCI). O algoritmo original do ecossistema MetaTrader 5 foi portado para o StockSharp e redesenhado para usar subscrições de velas de alto nível com ligações de indicadores. Dois pipelines de indicadores independentes são avaliados: um período lento de "tendência" que define o viés direcional e um período rápido de "sinal" que gera decisões de entrada e saída.

Em cada período a estratégia calcula um oscilador composto. O valor do oscilador é derivado das contribuições ponderadas de IBS, RSI e CCI. Mudanças rápidas no valor composto são suavizadas, limitadas por um limiar de momentum configurável e envolvidas por um envelope de volatilidade que imita a lógica de buffer do indicador original. Cruzamentos entre o valor composto e seu envelope suavizado são os gatilhos principais para as decisões.

### Lógica de negociação

1. **Detecção de tendência** – O período lento monitora o oscilador composto. Se o composto permanecer acima do envelope a estratégia marca uma tendência de alta, caso contrário sinaliza uma tendência de baixa.
2. **Geração de sinal** – O período rápido avalia dois valores consecutivos do composto e do envelope. Cruzamentos na barra mais recente confirmam um sinal acionável somente quando a barra anterior suporta a transição.
3. **Regras de entrada** –
   * Entrar comprado somente quando operações compradas são permitidas, a tendência atual é altista e o composto cruza abaixo do envelope no período rápido (reversão baixista para altista na orientação do indicador original).
   * Entrar vendido somente quando operações vendidas são permitidas, a tendência atual é baixista e o composto cruza acima do envelope no período rápido.
4. **Regras de saída** –
   * Saídas imediatas opcionais em cruzamentos do composto quando os interruptores `_CloseLongOnSignalCross` ou `_CloseShortOnSignalCross` estão habilitados.
   * Saídas forçadas baseadas em tendência quando `_CloseLongOnTrendFlip` ou `_CloseShortOnTrendFlip` solicitam fechamento assim que o viés do período lento se inverte.
   * O gerenciamento de risco é tratado pelo `StartProtection` do StockSharp, traduzindo as distâncias configuradas de stop loss e take profit em pontos para deslocamentos de preço absolutos usando o passo de preço do instrumento.

### Indicadores e cálculos

* **Internal Bar Strength (IBS):** `(close - low) / max(high - low, price step)` suavizado por uma média móvel selecionável.
* **RSI:** RSI padrão aplicado a um preço configurável (fechamento, abertura, máximo, mínimo, mediana, típico ou ponderado).
* **CCI:** Implementação personalizada de CCI com média móvel simples e estimador de desvio médio derivado do preço aplicado selecionado.
* **Oscilador composto:** Soma ponderada dos valores transformados de IBS, RSI e CCI dividida por três, limitada pela configuração `Threshold` para replicar o "limitador de momentum" original.
* **Envelope:** As leituras máximas e mínimas do composto ao longo do intervalo configurado são suavizadas duas vezes e calculadas em média para produzir a linha de base de sinal usada para os cruzamentos.

A implementação evita o polling direto de valores de indicadores (`GetValue`) mantendo todo o estado dentro das classes calculadoras e alimentando velas sequencialmente através da API de alto nível.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `OrderVolume` | Tamanho base da ordem ao abrir uma nova posição. |
| `TrendCandleType` | Tipo de vela para a subscrição do período lento. |
| `TrendIbsPeriod`, `TrendIbsMaType` | Período de suavização IBS e tipo de média móvel para o período lento. |
| `TrendRsiPeriod`, `TrendRsiPrice` | Período RSI e preço aplicado para o período lento. |
| `TrendCciPeriod`, `TrendCciPrice` | Período CCI e preço aplicado para o período lento. |
| `TrendThreshold` | Limiar de limitação de momentum usado no composto do período lento. |
| `TrendRangePeriod`, `TrendSmoothPeriod` | Intervalo de lookback e janela de suavização para o envelope do período lento. |
| `TrendSignalBar` | Deslocamento (número de velas fechadas atrás) usado ao ler valores do período lento. |
| `AllowLongEntries`, `AllowShortEntries` | Habilitar ou desabilitar novas operações compradas/vendidas. |
| `CloseLongOnTrendFlip`, `CloseShortOnTrendFlip` | Forçar saídas de posição quando o viés do período lento se inverte. |
| `SignalCandleType` | Tipo de vela para a subscrição do período rápido. |
| `SignalIbsPeriod`, `SignalIbsMaType` | Configuração de suavização IBS para o período rápido. |
| `SignalRsiPeriod`, `SignalRsiPrice` | Configurações RSI para o período rápido. |
| `SignalCciPeriod`, `SignalCciPrice` | Configurações CCI para o período rápido. |
| `SignalThreshold` | Limiar de limitação de momentum usado no composto do período rápido. |
| `SignalRangePeriod`, `SignalSmoothPeriod` | Intervalo do envelope e suavização no período rápido. |
| `SignalSignalBar` | Deslocamento aplicado ao avaliar sinais do período rápido. |
| `CloseLongOnSignalCross`, `CloseShortOnSignalCross` | Gatilhos de saída opcionais em cruzamentos do período rápido. |
| `StopLossPoints`, `TakeProfitPoints` | Distâncias de stop loss e take profit medidas em pontos de passo de preço. |

## Notas de uso

1. Configure o instrumento e os tipos de vela antes de iniciar a estratégia. Ambos os períodos serão subscritos automaticamente através de `GetWorkingSecurities`.
2. A configuração padrão espelha a versão MQL original: velas de tendência de 8 horas com velas de sinal de 1 hora e configurações de indicadores idênticas em ambos os períodos.
3. Como o oscilador composto é limitado internamente, períodos de volatilidade extrema podem produzir respostas mais planas do que estratégias de momentum típicas. Ajuste os parâmetros `Threshold`, `RangePeriod` e `SmoothPeriod` para adaptar a sensibilidade.
4. A proteção de posição integrada depende do `PriceStep` do instrumento. Certifique-se de que os metadados do instrumento fornecem um passo válido, caso contrário considere ajustar o fallback no código.
5. Use os helpers de gráficos do StockSharp se precisar visualizar o comportamento. A estratégia já desenha as velas do período de sinal e as operações executadas quando uma área de gráfico está disponível.

## Riscos e limitações

* A estratégia pressupõe entrega sequencial de velas. Atualizações de velas fora de ordem podem dessincronizar os buffers internos.
* O desvio médio no CCI personalizado é recalculado a partir dos valores em buffer; a precisão depende do recebimento de um fluxo de dados contínuo sem lacunas.
* Quando `OrderVolume` é combinado com exposição existente, as inversões serão realizadas enviando uma única ordem de mercado dimensionada para fechar a posição oposta e abrir a nova. Certifique-se de que as permissões da corretora permitem esse comportamento.
* O port preserva a orientação do indicador original (coeficientes negativos). Portanto, os sinais podem parecer contraintuitivos até que você revise o design do indicador legado.

## Ampliação da estratégia

* Ajuste os tipos de média móvel independentemente para o envelope e a suavização IBS para explorar reações mais rápidas ou lentas.
* Substitua o calculador CCI personalizado pelo indicador integrado do StockSharp se uma versão futura expuser os seletores de preço necessários.
* Adicione sobreposições de gráfico vinculando os valores compostos a painéis de gráfico adicionais quando mais feedback visual for necessário.
* Combine com controles de risco adicionais como perda diária máxima ou filtros de tempo de negociação para implantações em produção.

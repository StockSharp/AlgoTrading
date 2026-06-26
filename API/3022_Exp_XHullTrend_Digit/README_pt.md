# Estratégia Exp XHullTrend Digit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Conversão do especialista MQL5 `Exp_XHullTrend_Digit.mq5` localizado em `MQL/22117`.
- Usa a API de alto nível do StockSharp com o `XHullTrendDigitIndicator` personalizado que replica a lógica original do XHullTrend Digit.
- Focado no seguidor de tendência de médio prazo no período configurado do indicador (padrão de 8 horas).

## Lógica do indicador
1. O preço é obtido da fonte de vela selecionada (fechamento por padrão).
2. Duas médias móveis são calculadas com comprimentos `BaseLength` e `BaseLength / 2` usando o método de suavização escolhido (simples, exponencial, suavizado ou ponderado).
3. Uma projeção estilo Hull `2 * shortMA - longMA` é suavizada duas vezes: primeiro por `SignalLength`, depois por `sqrt(BaseLength)`.
4. Ambas as linhas resultantes são arredondadas para o múltiplo mais próximo do passo do instrumento escalonado por `10^RoundingDigits` para imitar o arredondamento de dígitos da versão MQL5.
5. Quando o arredondamento produz valores iguais enquanto os valores brutos diferem, a linha mais rápida é deslocada um passo na direção da diferença para que o cruzamento permaneça detectável.

## Regras de negociação
- Os sinais são avaliados apenas em velas fechadas.
- `SignalBar` define quantas barras atrás são usadas para a detecção do cruzamento (1 = usar a barra completada anterior contra a barra anterior a ela).
- Entrada comprada: linha rápida anterior acima da lenta **e** a linha rápida da barra selecionada em ou abaixo da lenta (cruzamento para cima). As posições vendidas são opcionalmente fechadas ao mesmo tempo.
- Entrada vendida: linha rápida anterior abaixo da lenta **e** a linha rápida da barra selecionada em ou acima da lenta (cruzamento para baixo). As posições compradas são fechadas simultaneamente de forma opcional.
- Saída comprada: quando a linha rápida anterior cai abaixo da lenta.
- Saída vendida: quando a linha rápida anterior sobe acima da lenta.
- Se um sinal de reversão aparecer enquanto mantém a posição oposta, a estratégia envia a ordem de fechamento seguida por uma ordem dimensionada para inverter a posição à nova direção.

## Parâmetros
- `OrderVolume` – volume para entradas de mercado.
- `StopLoss` / `TakeProfit` – distâncias de proteção opcionais em passos de preço (convertidas para `UnitTypes.Step` do StockSharp).
- `EnableBuyEntry`, `EnableSellEntry` – permitir ou bloquear novas posições em cada direção.
- `EnableBuyExit`, `EnableSellExit` – controlar saídas automáticas para lados comprado e vendido.
- `CandleType` – período usado para cálculos do indicador (período padrão de 8 horas).
- `BaseLength` – comprimento de suavização base para o indicador (equivale a `XLength` no MQL5).
- `SignalLength` – comprimento da suavização Hull intermediária (`HLength` no MQL5).
- `PriceSource` – preço de vela usado para cálculos (fechamento/abertura/máximo/mínimo/típico/ponderado/mediano/médio).
- `SmoothMethod` – tipo de média móvel para todas as etapas de suavização (simples, exponencial, suavizado, ponderado).
- `Phase` – mantido para compatibilidade; sem efeito com os tipos de suavização suportados.
- `RoundingDigits` – número de ajustes de dígitos adicionais aplicados durante o arredondamento.
- `SignalBar` – deslocamento de barra para avaliação de sinais (0 = barra fechada atual, 1 = barra anterior, etc.).

## Gestão de risco
- Stop loss e take profit opcionais gerenciados pelo helper `StartProtection` incorporado usando distâncias baseadas em passos.
- O volume pode ser ajustado via `OrderVolume` para corresponder ao tamanho do instrumento alvo.

## Notas
- O indicador personalizado reproduz o comportamento de arredondamento do script original; certifique-se de que `Security.PriceStep` esteja configurado para arredondamento preciso.
- Apenas os suavizamentos SMA, EMA, SMMA (RMA) e LWMA são implementados porque a biblioteca padrão do StockSharp os fornece de série. Outros modos de suavização exóticos da fonte MQL5 podem ser adicionados posteriormente se necessário.
- Funciona em qualquer instrumento que forneça velas para o período selecionado. Ajuste dígitos de arredondamento e comprimento base ao alternar entre ativos com diferentes tamanhos de tick.

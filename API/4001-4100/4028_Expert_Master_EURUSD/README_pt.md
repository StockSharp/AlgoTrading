# Estratégia Mestre Especialista EURUSD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia Expert Master EURUSD replica o MetaTrader 4 Expert Advisor *Expert Master*.
Ele avalia um padrão de quatro velas nas linhas MACD principal e de sinal (EMA rápida = 5, EMA lenta = 15, sinal EMA = 3).
O algoritmo espera que o indicador crie impulso em uma direção antes de desencadear uma entrada de rompimento na direção oposta.

## Lógica de negociação

### Configuração longa
1. A linha de sinal MACD forma uma sequência descendente nas três velas anteriores e vira para cima na vela atual.
2. A linha principal MACD forma um "V" onde o valor atual está acima das três leituras anteriores.
3. O valor da linha principal anterior está abaixo do limite inferior configurável (padrão −0,00020).
4. O valor da linha principal mais antigo está abaixo de zero enquanto o valor atual está acima do limite superior (padrão 0,00020).

### Configuração curta
1. A linha de sinal MACD forma uma sequência ascendente nas três velas anteriores e desce na vela atual.
2. A linha principal MACD forma um "V" invertido onde o valor atual está abaixo das três leituras anteriores.
3. O valor da linha principal anterior excede o limite superior (padrão 0,00020).
4. O valor da linha principal mais antigo está acima de zero enquanto o valor atual cai abaixo do limite curto (padrão −0,00035).

## Gerenciamento de posição

- **Saída em caso de perda de impulso:** Uma posição longa é fechada quando o valor principal atual de MACD cai abaixo do anterior.
As posições curtas são fechadas quando o valor principal atual de MACD sobe acima do anterior.
- **Trailing Stop:** Depois que o preço se move pelo número configurado de pontos a favor da negociação, um trailing stop é ativado.
O stop é atualizado em cada vela finalizada usando o fechamento da vela menos/mais a distância final.
Se o preço retornar ao trailing stop, a estratégia sai por meio de uma ordem de mercado.

## Gestão de risco

- O volume de negociação é padronizado para o tamanho de lote fixo, mas pode ser ajustado dinamicamente por meio do parâmetro **Porcentagem de Risco**.
Quando o dimensionamento de risco está ativado, a estratégia arrisca uma fração do valor do portfólio em cada entrada, imitando o comportamento original EA.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `TrailingPoints` | Distância do trailing stop em faixas de preço. | 25 |
| `FixedVolume` | Volume de negociação de reserva quando o dimensionamento do risco não estiver disponível. | 1 |
| `RiskPercent` | Percentual do valor do portfólio utilizado para dimensionar posições. | 0,01 |
| `MacdFastPeriod` | Comprimento EMA rápido para a linha principal MACD. | 5 |
| `MacdSlowPeriod` | Comprimento EMA lento para a linha principal MACD. | 15 |
| `MacdSignalPeriod` | Comprimento do sinal EMA para o indicador MACD. | 3 |
| `UpperMacdThreshold` | Limite positivo de MACD necessário para entradas. | 0,00020 |
| `LowerMacdThreshold` | Limite negativo MACD usado em sinais longos. | −0,00020 |
| `ShortCurrentThreshold` | Limite negativo de MACD aplicado ao valor atual para shorts. | −0,00035 |
| `CandleType` | Tipo de vela usado para cálculos de indicadores. | Período de 1 minuto |

## Notas

- Negocie apenas em velas acabadas para ficar alinhado com o StockSharp API de alto nível.
- A conversão mantém a lógica original EA, incluindo dimensionamento de lote baseado em risco e comportamento de trailing-stop, ao mesmo tempo em que adiciona parametrização extensiva para otimização mais fácil.

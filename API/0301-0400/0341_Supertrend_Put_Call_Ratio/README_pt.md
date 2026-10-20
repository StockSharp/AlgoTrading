# Supertrend Put Call Ratio

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Regras de negociação

Usar SuperTrend nativo do StockSharp com Period e Multiplier. A partir de zero, comprar se Close > SuperTrend e PCR < média anterior − PCRMultiplier × σ; vender se Close < SuperTrend e PCR > média anterior + PCRMultiplier × σ. PCR é volume put / volume call observado. Fechar comprado com Close < SuperTrend e vendido com Close > SuperTrend, independentemente do PCR. Igualdade não gera sinal.

A média e o desvio padrão amostral usam as N observações consecutivas anteriores sem a atual: σ = sqrt(Σ(x − mean)² / (N − 1)). Exige-se uma janela completa. Não há valores aleatórios nem substitutos derivados de preços.

## Dados de entrada

O CSV obrigatório usa SecurityId,Time,Value. Time é a abertura UTC da vela de sinal (yyyy-MM-ddTHH:mm:ssZ). Valores observados são finitos, não negativos e únicos por instante. Usa-se somente a observação atual. Uma lacuna limpa a janela e bloqueia entradas até novo aquecimento; saídas permanecem ativas.

## Parâmetros

| Parâmetro | Valor padrão |
|---|---|
| `Period` | `10` |
| `Multiplier` | `3` |
| `PCRPeriod` | `20` |
| `PCRMultiplier` | `2` |
| `PCRHistoryFile` | obrigatório |
| `AtrPeriod` | `14` |
| `StopLossAtr` | `2` |
| `CandleType` | `TimeSpan.FromMinutes(15).TimeFrame()` |

## Execução

AtrPeriod = 14 usa ATR de Wilder. Na entrada a partir de zero fixa-se ATR(sinal) × StopLossAtr. A proteção local nativa por ordens a mercado segue cotações, ancora a distância na execução real e pode fechar entre velas. Alterações posteriores no ATR não movem o stop. Zero desativa a proteção para uma nova execução. O sinal inverso fecha toda a posição real sem reversão ou aumento na mesma decisão.

## Verificação

As execuções usam apenas o arquivo BTC/TON existente. Relatórios, observações de opções e histórico de aquecimento explícitos verificam a mecânica e não são dados reais de empresas ou opções. Janelas menores, dois grupos e março como mês anual são perfis de teste identificados, não prova dos padrões originais de longo prazo. Não se afirma rentabilidade.


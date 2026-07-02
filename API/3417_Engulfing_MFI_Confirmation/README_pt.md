# Estratégia envolvente de confirmação de IMF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o especialista MetaTrader "Expert_ABE_BE_MFI" combinando padrões de engolfo de velas japonesas com a confirmação do oscilador Money Flow Index (MFI). Uma posição longa é aberta quando uma vela envolvente de alta aparece enquanto o fluxo de dinheiro permanece em uma zona de sobrevenda. Uma posição curta é aberta quando uma vela de baixa se forma sob condições de fluxo de dinheiro sobrecomprado. As posições são fechadas quando a IMF ultrapassa os limites de saída dinâmicos, sinalizando reversões de impulso.

## Ideia Central

1. **Pattern detection** – the body of the current finished candle must fully engulf the previous candle in the direction of the trade.
2. **Confirmação de volume** – o indicador MFI (comprimento configurável, padrão 37) deve estar abaixo do nível de sobrevenda (40) para entradas longas ou acima do nível de sobrecompra (60) para entradas curtas.
3. **Saídas dinâmicas** – as posições abertas são fechadas quando a IMF cruza os principais níveis de reversão (30 e 70) na direção oposta, imitando a lógica de votação original do especialista MQL.

## Indicadores

- **Índice de fluxo de dinheiro (MFI)** – calcula o momentum ajustado pelo volume. A estratégia armazena as duas últimas leituras da MFI para detectar passagens de nível.
- **Análise Corporal de Vela** – nenhum indicador adicional é registrado; a detecção de engolfamento usa as duas últimas velas concluídas.

## Regras de negociação

### Entrada longa

- A vela anterior é de baixa e a vela atual é de alta.
- O corpo da vela atual abre abaixo ou igual ao fechamento anterior e fecha acima ou igual à abertura anterior (engolfo estrito).
- Latest MFI value is below the configurable `OversoldLevel` (default 40).

### Entrada curta

- A vela anterior é de alta e a vela atual é de baixa.
- Current candle body opens above or equal to the previous close and closes below or equal to the previous open.
- O valor MFI mais recente está acima do configurável `OverboughtLevel` (padrão 60).

### Condições de saída

- **Fechar Short** quando a IMF cruza acima de `ExitLongLevel` (30) ou `ExitShortLevel` (70) de baixo.
- **Fechar Long** quando a IMF cruza abaixo de `ExitShortLevel` (70) ou `ExitLongLevel` (30) de cima.

Estes limiares de saída recriam a lógica de voto duplo do perito original, garantindo que movimentos prolongados no fluxo de dinheiro desencadeiam a liquidação atempada de posições.

### Gestão Comercial

- As ordens de mercado (`BuyMarket` / `SellMarket`) são utilizadas para entradas e saídas.
- No explicit stop-loss or take-profit is used; a gestão do risco depende dos sinais de reversão das IMFs.

## Parâmetros

| Nome | Descrição | Padrão | Faixa/Notas |
| ---- | ----------- | ------- | ------------- |
| `CandleType` | Candle timeframe used for analysis. | 1 minuto | Qualquer tipo de vela compatível. |
| `MfiPeriod` | Length of the Money Flow Index. | 37 | Deve ser > 0; matches original EA default. |
| `OversoldLevel` | Nível de IMF que confirma configurações de alta envolvente. | 40 | Ative a otimização, se necessário. |
| `OverboughtLevel` | Nível de MFI que confirma configurações de baixa. | 60 | Enable optimization if needed. |
| `ExitLongLevel` | Lower MFI boundary for detecting reversals. | 30 | Usado para saídas longas e confirmações curtas. |
| `ExitShortLevel` | Upper MFI boundary for detecting reversals. | 70 | Used for both short exits and long confirmations. |

## Notas sobre conversão

- O especialista original MQL agregou “votos” de padrões envolventes e filtros MFI. A estratégia C# reproduz o mesmo fluxo de decisão convertendo diretamente as regras de votação em condições discretas de entrada e saída.
- Money management and trailing modules from the MQL version are omitted; O dimensionamento da posição StockSharp é controlado pelo volume da estratégia.
- Todas as vinculações de indicadores aproveitam o API (`SubscribeCandles().Bind(...)`) de alto nível conforme necessário.

## Dicas de uso

- Otimize `MfiPeriod`, `OversoldLevel` e `OverboughtLevel` para adaptar a estratégia a mercados específicos.
- Combine com controles de risco (paradas de proteção) via `StartProtection` no aplicativo host se segurança adicional for necessária.
- Garanta dados históricos suficientes para que o Índice de Fluxo de Dinheiro esteja totalmente formado antes de permitir a negociação.

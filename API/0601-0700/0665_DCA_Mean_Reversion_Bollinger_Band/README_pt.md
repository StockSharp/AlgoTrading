# Estratégia DCA de Reversão à Média com Bollinger Band
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Compra um valor fixo em dólares quando o preço cruza abaixo da banda inferior de Bollinger ou no primeiro dia de cada mês. Todas as posições são fechadas em uma data especificada.

### Regras exatas de C# e Python

Padrões: `InvestmentAmount=200` unidades da **moeda cotada**, `OpenDate=DateTime.MinValue`, `CloseDate=DateTime.MaxValue`, `StrategyMode=Combined`, `BollingerPeriod=20`, `BollingerMultiplier=2`, `CandleType=5 minutos`. Em **candles concluídos**, a banda inferior é SMA20 menos dois desvios padrão. Compra Bollinger quando antes `Close >= banda inferior anterior` e agora `Close < banda inferior atual`. Monthly compra **uma vez por mês**, no primeiro candle elegível após aquecer o indicador. Modos: `Bollinger` (somente cruzamento), `Monthly` (mensal) e `Combined` (qualquer condição, uma ordem se ambas coincidirem). Dentro de `OpenDate <= data < CloseDate`, compra `floor((InvestmentAmount / Close) / VolumeStep) × VolumeStep`, respeitando limites do instrumento. Somente **acumula long**, nunca short. A partir de CloseDate liquida a **posição restante inteira** no primeiro candle disponível e para de comprar. Sem stop, short ou cruzamento EMA; slippage pode afetar o valor executado. O README original não indicava defaults numéricos; foram definidos explicitamente nesta implementação.



## Parâmetros
- `InvestmentAmount` - valor investido a cada vez
- `OpenDate` - data de início das compras
- `CloseDate` - data para fechar todas as posições
- `StrategyMode` - reversão à média BB, DCA mensal ou combinado
- `BollingerPeriod` - período das Bollinger Bands
- `BollingerMultiplier` - multiplicador de desvio padrão
- `CandleType` - período para o cálculo de Bollinger

## Indicadores
- Bollinger Bands

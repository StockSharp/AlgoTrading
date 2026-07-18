# Estratégia de Negociação Automática com RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia calcula a média dos últimos valores de RSI para gerar sinais de trading. Calcula um Índice de Força Relativa (RSI) padrão sobre um período configurável e depois aplica uma média móvel simples ao próprio RSI. As operações são abertas quando o RSI médio cruza limites predefinidos e fechadas quando o limiar oposto é atingido.

## Lógica de trading

1. **Cálculo do RSI**
   - O indicador usa `RsiPeriod` para calcular o RSI baseado nos preços de fechamento das velas.
2. **Média do RSI**
   - Os últimos `AveragePeriod` valores de RSI são suavizados por uma média móvel simples.
3. **Regras de entrada**
   - Se `BuyEnabled` for `true` e nenhuma posição estiver aberta, uma ordem de **compra** é enviada quando o RSI médio excede `BuyThreshold` (padrão 55).
   - Se `SellEnabled` for `true` e nenhuma posição estiver aberta, uma ordem de **venda** é enviada quando o RSI médio cai abaixo de `SellThreshold` (padrão 45).
4. **Regras de saída**
   - Quando `CloseBySignal` for `true`, posições abertas são fechadas em sinais opostos:
     - Posições compradas fecham quando o RSI médio cai abaixo de `CloseBuyThreshold` (padrão 47).
     - Posições vendidas fecham quando o RSI médio sobe acima de `CloseSellThreshold` (padrão 52).

## Parâmetros

- `BuyEnabled` – habilitar ou desabilitar entradas compradas.
- `SellEnabled` – habilitar ou desabilitar entradas vendidas.
- `CloseBySignal` – permitir saídas em sinais RSI opostos.
- `RsiPeriod` – comprimento do cálculo do RSI.
- `AveragePeriod` – número de valores de RSI usados para a média.
- `BuyThreshold` – valor do RSI médio acima do qual uma posição comprada é aberta.
- `SellThreshold` – valor do RSI médio abaixo do qual uma posição vendida é aberta.
- `CloseBuyThreshold` – valor do RSI médio abaixo do qual uma posição comprada é fechada.
- `CloseSellThreshold` – valor do RSI médio acima do qual uma posição vendida é fechada.
- `CandleType` – tipo de vela para assinaturas.

## Notas

Esta estratégia demonstra como valores de indicadores podem ser combinados por vinculação na API de alto nível do StockSharp. Funções de trailing stop e gerenciamento de dinheiro da versão MQL original são omitidas por simplicidade.


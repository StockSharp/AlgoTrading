# Estratégia de Secwenta MultiBar Signals
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
Esta estratégia é um port do StockSharp do consultor especializado MetaTrader "Secwenta" (ID MQL 22977). O algoritmo escaneia candles concluídos e conta quantos fecharam em alta (fechamento > abertura) ou em baixa (fechamento < abertura) dentro de um histórico rolante curto. Dependendo da configuração, pode operar nos modos somente compra, somente venda ou reversão bidirecional. Quando o número necessário de barras de alta ou baixa aparece, a estratégia abre ou fecha posições de mercado usando um volume fixo que espelha a configuração de lote original.

## Avaliação de sinais
- Apenas candles terminados do `CandleType` selecionado são processados via API de assinatura de alto nível.
- Para cada candle a estratégia registra se foi de alta, baixa ou neutro (doji). O buffer interno mantém as últimas *N* direções, onde *N* é o maior entre `BullishBarCount` e `BearishBarCount` entre os lados habilitados (compra e/ou venda).
- O contador de alta incrementa quando um candle fecha acima de sua abertura, enquanto o contador de baixa incrementa em fechamentos abaixo da abertura. Candles neutros não afetam os contadores.
- Um sinal é acionado uma vez que o contador correspondente atinge seu limiar configurado dentro da janela rolante. Isso reproduz a lógica MQL original que iterava pelas barras mais recentes até encontrar o número solicitado de candles de alta ou baixa.

## Regras de negociação
1. **Modo somente compra (`UseBuySignals = true`, `UseSellSignals = false`):**
   - Quando o contador de baixa atinge `BearishBarCount`, qualquer posição comprada existente é fechada com uma ordem de venda a mercado.
   - Quando o contador de alta atinge `BullishBarCount` e a estratégia está zerada, uma nova posição comprada é aberta usando `OrderVolume`.
2. **Modo somente venda (`UseBuySignals = false`, `UseSellSignals = true`):**
   - Quando o contador de alta atinge `BullishBarCount`, uma posição vendida aberta é coberta com uma ordem de compra a mercado.
   - Quando o contador de baixa atinge `BearishBarCount` e a estratégia está zerada, uma nova posição vendida é aberta usando `OrderVolume`.
3. **Modo de reversão (`UseBuySignals = true` e `UseSellSignals = true`):**
   - Um gatilho de alta fecha qualquer exposição vendida e, se a estratégia não estiver já comprada, abre uma nova posição comprada comprando `OrderVolume` mais o tamanho absoluto da posição vendida. Isso imita a sequência original de fechar vendas antes de abrir compras.
   - Um gatilho de baixa fecha qualquer exposição comprada e, se a estratégia não estiver já vendida, abre uma nova posição vendida vendendo `OrderVolume` mais o tamanho absoluto da posição comprada.

Todas as operações de mercado reutilizam os helpers `BuyMarket` e `SellMarket` do StockSharp, e a estratégia chama `StartProtection()` para que proteções no nível de conta possam ser sobrepostas se desejado.

## Parâmetros
| Parâmetro | Descrição | Padrão | Notas |
|-----------|-----------|--------|-------|
| `CandleType` | Tipo de dados de candle (período) usado para avaliar sequências. | Período de 1 hora | Qualquer tipo de candle suportado pelo StockSharp pode ser selecionado. |
| `OrderVolume` | Volume base da ordem de mercado que espelha o tamanho de lote MQL. | 1 | Adicionado ao volume de fechamento ao reverter uma posição. |
| `UseBuySignals` | Habilita o processamento de sinais de alta. | `true` | Quando desabilitado, nenhum novo trade comprado é aberto. |
| `BullishBarCount` | Número de candles de alta necessários para acionar um evento de alta. | 2 | Deve permanecer consistente com o limiar de fechamento ao executar no modo somente compra. |
| `UseSellSignals` | Habilita o processamento de sinais de baixa. | `false` | Quando desabilitado, nenhum novo trade vendido é aberto. |
| `BearishBarCount` | Número de candles de baixa necessários para acionar um evento de baixa. | 1 | Atua tanto como limiar de abertura para vendidos quanto como limiar de saída para comprados. |

## Notas de implementação
- A janela rolante usa uma fila para manter as últimas direções de candles e garante que os contadores correspondam ao tamanho da janela mesmo após mudanças de parâmetros.
- Apenas candles terminados são processados para manter fidelidade ao tratamento original de eventos "nova barra".
- Candles neutros (doji) deixam os contadores inalterados, exatamente como no código MQL.
- As reversões são executadas com uma única ordem de mercado que combina o volume de fechamento e abertura, mantendo mudanças de exposição deterministas.
- O comprimento do buffer é igual ao maior limiar ativo; se um lado está desabilitado, apenas o limiar correspondente contribui para o comprimento do lookback, correspondendo ao comportamento de `CopyRates` na versão MQL.

## Arquivos
- `CS/SecwentaMultiBarSignalsStrategy.cs` – implementação principal em C# construída sobre a API de estratégia de alto nível do StockSharp.

> **Nota:** Nenhuma tradução para Python é fornecida para este ID; apenas a versão C# solicitada está disponível.

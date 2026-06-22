# Estratégia Alternante Cheduecoglioni
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um port para StockSharp do expert advisor MQL5 "cheduecoglioni". Ela mantém o trader sempre no mercado alternando entre posições vendidas e compradas. Cada entrada é protegida com níveis fixos de take-profit e stop-loss definidos em pips e convertidos para deslocamentos de preço de acordo com a precisão do instrumento.

## Regras de negociação
- A estratégia ouve a série de candles configurada (1 minuto por padrão) e só reage quando um candle está completamente fechado. Este evento substitui o loop baseado em ticks do expert advisor original.
- Quando não há posição aberta e nenhuma ordem a mercado aguardando execução, a estratégia envia uma ordem a mercado na direção armazenada no estado `_nextSide`. O primeiro trade após o início é uma venda, correspondendo à implementação MQL5.
- Assim que uma posição fica ativa, o algoritmo aguarda seu fechamento pelas ordens de proteção ou intervenção manual. Assim que a posição retorna a zero, a próxima direção é invertida, portanto o trade seguinte será na direção oposta.
- As distâncias de stop-loss e take-profit são aplicadas automaticamente por `StartProtection`, garantindo que cada trade carregue as distâncias de risco-recompensa configuradas.

## Parâmetros
- `Trade Volume` – volume usado para cada entrada a mercado. Isso espelha o input `InpLots`.
- `Take Profit (pips)` – distância em pips para a ordem take-profit. A estratégia a converte em distância de preço absoluta usando o tamanho de pip detectado.
- `Stop Loss (pips)` – distância em pips para o stop loss de proteção, convertido com a mesma lógica de tamanho de pip.
- `Candle Type` – período dos candles que impulsionam o ciclo de decisão. Qualquer `DataType` suportado pode ser fornecido.

## Detalhes de implementação
- O tamanho do pip é derivado de `Security.PriceStep`. Para símbolos FX de 3 ou 5 dígitos, o valor é multiplicado por 10 para passar do pip fracionário ao pip padrão, replicando o ajuste MQL.
- Um sinalizador de espera previne ordens a mercado duplicadas enquanto uma ordem anterior aguarda execução. Se o broker rejeitar a ordem, `OnOrderFailed` limpa o sinalizador para que o próximo candle possa tentar novamente.
- `OnPositionChanged` acompanha o lado da posição ativa e alterna `_nextSide` após cada estado plano. Isso reflete a lógica MQL que abria o lado oposto após cada saída.
- As ordens de proteção são gerenciadas por `StartProtection` com saídas a mercado, correspondendo à atribuição imediata de stop-loss e take-profit que o expert advisor realizava ao colocar a ordem.

## Notas
- A versão Python ainda não foi criada intencionalmente.
- A estratégia não modifica testes unitários.

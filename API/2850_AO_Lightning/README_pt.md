# Estratégia AO Relâmpago
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

AO Relâmpago reproduz o assessor especialista MT5 "AO_Lightning" usando a API de alto nível do StockSharp. O sistema monitora a inclinação do Awesome Oscillator (AO) construído a partir de preços medianos. Quando o oscilador diminui, a estratégia acumula exposição comprada, e quando o oscilador aumenta, ela constrói uma posição vendida. As posições são piramidadas até um limite configurável enquanto posições opostas são fechadas antes de mudar de direção.

## Lógica de trading

1. Subscrever a série de velas selecionada e calcular o Awesome Oscillator com período curto 5 e período longo 34 (os padrões retirados do código MQL original).
2. Aguardar apenas velas terminadas; a estratégia ignora atualizações intermediárias para evitar dupla contagem.
3. Na primeira vela terminada, o valor AO é armazenado como referência.
4. Quando o valor AO atual é **menor** que o valor anterior:
   - Se existir uma posição vendida aberta, enviar uma ordem de compra de mercado dimensionada para fechar todo o vendido e imediatamente adicionar uma camada comprada.
   - Se não houver vendido e a exposição comprada estiver abaixo do limite, comprar uma camada adicional.
5. Quando o valor AO atual é **maior** que o valor anterior:
   - Se existir uma posição comprada aberta, enviar uma ordem de venda de mercado que fecha a exposição comprada e simultaneamente abre uma camada vendida.
   - Se não houver comprado e a exposição vendida estiver abaixo do limite, vender uma camada adicional.
6. Valores AO iguais ao valor anterior deixam a posição sem alteração.
7. O `StartProtection()` integrado é habilitado uma vez na inicialização para que os usuários do Designer possam anexar stops ou outros módulos de risco, se desejado.

A lógica reflete o assessor especialista original: a inclinação do AO define a direção do trade, os trades opostos são aplainados antes de uma nova entrada, e as ordens incrementais continuam se acumulando até o limite ser atingido.

## Gerenciamento de posição

- **Volume de trade** define o tamanho de cada camada adicional e corresponde ao parâmetro MT5 `LotFixed`.
- **Máximo de posições** corresponde ao input MT5 `Orders`. Ele restringe quantas camadas podem se acumular em qualquer lado.
- **Piramidação** é linear: cada sinal válido adiciona exatamente uma camada do tamanho de um lote, desde que o limite não tenha sido atingido.
- **Aplainamento** envia ordens combinadas (fechar + nova direção) para evitar estados planos intermediários ao mudar de vendido para comprado ou vice-versa.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `TradeVolume` | Tamanho da ordem para cada nova camada. | 1 |
| `MaxPositions` | Número máximo de camadas compradas ou vendidas que podem estar ativas simultaneamente. | 10 |
| `AoShortPeriod` | Comprimento da SMA rápida usada pelo Awesome Oscillator (SMA de preço mediano). | 5 |
| `AoLongPeriod` | Comprimento da SMA lenta para o Awesome Oscillator. | 34 |
| `CandleType` | Fonte de dados de velas processada pela estratégia. | Período de 5 minutos |

## Notas

- O especialista MT5 original nomeia as entradas `Period_sma_slow` e `Period_sma_fast` mas troca os valores (5 e 34). O porto StockSharp mantém o mapeamento funcional expondo parâmetros intuitivos `AoShortPeriod`/`AoLongPeriod`.
- Nenhuma versão Python é fornecida ainda, conforme a solicitação da tarefa.
- Nenhum teste está incluído; execute as validações necessárias via Designer ou seu próprio conjunto de backtesting antes de implantar em produção.

# Estratégia de Hedge EES Hedger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia EES Hedger espelha o comportamento do assessor especializado clássico do MetaTrader que cobre automaticamente posições criadas por outro sistema de trading ou por traders manuais. Sempre que a conta monitorada abre uma posição que corresponde ao filtro configurado, a estratégia abre imediatamente uma posição oposta com seus próprios parâmetros. Dessa forma, neutraliza a exposição direcional enquanto ainda permite que a operação original continue.

O algoritmo é construído sobre a API de alto nível do StockSharp. Ele escuta as operações da conta, abre posições de hedge e gerencia ordens de proteção por meio de lógica de stop-loss, take-profit e trailing stop. O gerenciamento do trailing segue de perto a implementação original, avançando o stop somente quando o movimento do preço supera tanto a distância do stop quanto o incremento do trailing.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `HedgeVolume` | Volume fixo para a ordem de hedge. Não depende do tamanho da operação externa. |
| `StopLossPips` | Distância em pips para o stop-loss de proteção do hedge. Definir como zero para pular o stop inicial. |
| `TakeProfitPips` | Distância em pips para a ordem de take-profit. Definir como zero para omitir o alvo. |
| `TrailingStopPips` | Distância em pips usada para o trailing assim que o preço se move favoravelmente. |
| `TrailingStepPips` | Movimento mínimo em pips necessário antes de mover novamente o trailing stop. Deve ser positivo quando o trailing estiver ativo. |
| `OriginalOrderComment` | Filtro de comentário opcional. Apenas operações cujo comentário corresponda a este valor (sem distinção de maiúsculas/minúsculas) serão cobertas. Deixar vazio para reagir a todas as operações. |
| `HedgerOrderComment` | Comentário opcional usado para reconhecer as próprias operações de hedge da estratégia. Quando fornecido, operações com o mesmo comentário são ignoradas para evitar novo hedge. |

## Comportamento

1. **Detecção de operações** – a estratégia se subscreve aos eventos `NewMyTrade` do conector. Cada operação que vem do instrumento selecionado e passa pelos filtros de comentário é tratada como um sinal de entrada externo.
2. **Execução do hedge** – assim que uma operação qualificada é vista, a estratégia envia uma ordem de mercado na direção oposta usando `HedgeVolume`.
3. **Configuração de proteção** – após cada preenchimento próprio, o algoritmo cancela as ordens de proteção existentes e registra novas ordens de stop-loss e take-profit de acordo com o preço médio da posição atual.
4. **Trailing stop** – cada tick de operação recebido é usado para avaliar as regras de trailing. Assim que o preço se moveu pelo menos `TrailingStopPips + TrailingStepPips` em favor do hedge, o stop é aproximado do preço. Para posições compradas o stop segue abaixo do mercado, para vendidas acima.
5. **Redefinição de posição** – quando a posição de hedge está totalmente fechada (por exemplo, por stop ou alvo), a estratégia cancela automaticamente as ordens de proteção restantes e aguarda a próxima operação externa.

## Notas de uso

- A estratégia assume que o conector de conta reporta todas as operações da conta, incluindo as geradas por outros sistemas.
- O cálculo de pips se adapta ao passo de preço do instrumento e multiplica por dez para cotações de 3 ou 5 dígitos, imitando o ajuste de ponto MQL.
- Definir `OriginalOrderComment` para corresponder ao comentário do sistema primário se apenas operações específicas devem ser espelhadas. Ao cobrir operações manuais, deixar vazio.
- Garantir que `TrailingStepPips` permaneça maior que zero sempre que o trailing estiver habilitado para evitar encerramento prematuro na inicialização.
- Como o hedge sempre usa um volume fixo, pode ser conveniente ajustar `HedgeVolume` para que o hedge cubra a exposição média gerada pelo sistema primário.

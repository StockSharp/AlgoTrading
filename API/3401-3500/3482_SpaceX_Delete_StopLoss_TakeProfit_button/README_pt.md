# Estratégia do botão SpaceX Excluir StopLoss TakeProfit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia reproduz o botão **"DELETE SL_TP"** do painel MetaTrader original *SpaceX_Delete_StopLoss_TakeProfit_button.mq5*. Ele foi projetado como um utilitário auxiliar que verifica o portfólio atual e cancela todas as ordens protetoras ativas de stop-loss ou take-profit que pertencem a posições abertas. A conversão tem como alvo o StockSharp API de alto nível e fornece uma maneira conveniente de remover colchetes de proteção sem abrir manualmente cada ticket.

A estratégia não abre ou fecha posições por si só. Ele simplesmente inspeciona as negociações já abertas e remove suas ordens de proteção quando instruído a fazê-lo. Isto o torna adequado para traders que gerenciam posições manualmente ou por meio de outros sistemas automatizados, mas desejam um botão de pânico rápido que limpe todas as ordens de stop e take-profit.

## Consultor Especialista Original
A versão MetaTrader desenha uma única janela de diálogo com um botão **DELETE SL_TP**. Sempre que o botão é pressionado, o especialista percorre todas as posições abertas e chama `PositionModify` com valores zero para stop-loss e take-profit. Como resultado, cada nível de proteção é destacado da posição enquanto o volume da posição permanece inalterado.

Principais comportamentos do código-fonte:

* Nenhuma entrada ou saída de mercado é criada.
* Todos os símbolos no terminal são processados sem filtragem.
* Apenas os valores de stop-loss e take-profit são removidos; os comentários do pedido e os números mágicos permanecem intactos.
* A ação é acionada exclusivamente pelo botão GUI.

## StockSharp Implementação
A conversão StockSharp mantém o comportamento focado na remoção de ordens de proteção. Em vez de uma caixa de diálogo GUI, a ação é orientada por parâmetros de estratégia que podem ser alternados na interface do usuário StockSharp ou no código. A estratégia funciona com qualquer adaptador de corretora que exponha informações de stop de ordem ou de lucro.

Dois modos de execução são suportados:

1. **Execução automática na inicialização** – opcional. Quando ativada, a estratégia remove as ordens de proteção imediatamente após começar a ser executada.
2. **Comando manual** – um parâmetro booleano que imita o botão original. Definir o parâmetro como `true` agenda uma limpeza no próximo tique do temporizador, após o qual o sinalizador é redefinido para `false`.

A conversão cancela as ordens de proteção chamando `CancelOrder` em cada ordem ativa identificada como stop-loss, take-profit ou qualquer outra ordem de proteção condicional. Os volumes de posição nunca são tocados.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| **Executar ao iniciar** (`ApplyOnStart`) | Quando `true` a estratégia remove as ordens de proteção imediatamente após o início da estratégia. | `true` |
| **Todos os títulos** (`AffectAllSecurities`) | Processa todas as posições do portfólio. Quando `false` apenas a segurança da estratégia é considerada. | `true` |
| **Solicitação de exclusão** (`DeleteRequest`) | Gatilho manual que emula o botão MetaTrader. Mude para `true` para realizar uma remoção única; ele reinicia automaticamente. | `false` |
| **Intervalo(s) de pesquisa** (`PollingIntervalSeconds`) | Intervalo do temporizador em segundos usado para pesquisar o acionamento manual. O cronômetro também executa a solicitação de exclusão quando `Run On Start` está desabilitado. | `1` |

## Como funciona
1. No início, a estratégia valida o intervalo de pesquisa e inicia um cronômetro que é ativado a cada *N* segundos.
2. Se **Run On Start** estiver ativado, uma limpeza imediata será executada.
3. Cada tique do cronômetro verifica o sinalizador **Delete Request**. Quando a flag é `true` a estratégia coleta os títulos que possuem posições abertas dentro do escopo configurado e cancela todas as ordens de proteção desses instrumentos.
4. Após a execução, o sinalizador manual é redefinido para `false`, garantindo que a ação seja executada apenas uma vez por solicitação.

### Identificando ordens de proteção
Uma ordem é tratada como protetora quando qualquer uma das seguintes condições for atendida:

* O tipo de pedido é `Stop`, `TakeProfit` ou `Conditional`.
* Um preço stop, preço take-profit ou condição de ordem não nula está presente.

Esta definição conservadora abrange os adaptadores mais comuns. Se um conector usar tipos de pedidos personalizados ou condições para gerenciamento de parada, estenda a lógica de detecção adequadamente.

## Dicas de uso
* Anexe a estratégia ao conector que gerencia suas negociações abertas. Certifique-se de que todas as posições que você deseja gerenciar estejam visíveis no portfólio configurado.
* Acione a solicitação de exclusão da grade de parâmetros no Hydra ou Terminal alternando a caixa de seleção **Delete Request**.
* Combine o utilitário com outras estratégias para remover temporariamente os bráquetes de proteção antes de aplicar novos.
* Mantenha o intervalo de pesquisa pequeno (1 segundo por padrão) para uma experiência de botão responsiva. Aumente-o se quiser reduzir a atividade do temporizador.

## Diferenças em comparação com o original EA
* O botão MetaTrader atua instantaneamente por meio de uma caixa de diálogo de gráfico. Em StockSharp a ação é exposta como um parâmetro monitorado por um temporizador.
* As ordens de proteção são canceladas em vez de modificar os objetos de posição. Esta é a abordagem natural em StockSharp porque os níveis de stop-loss e take-profit são representados como ordens separadas em vez de propriedades de posição inline.
* O controle opcional do osciloscópio permite limitar a operação à segurança anexada, o que é uma conveniência extra em comparação com o especialista original.

## Limitações
* A estratégia exige que o adaptador exponha ordens de stop-loss e take-profit como ordens ativas. Se o corretor usar níveis de proteção do lado do servidor que não sejam representados como pedidos, talvez não seja possível cancelá-los.
* Nenhuma caixa de diálogo GUI é criada. O controle é realizado inteiramente através de parâmetros estratégicos ou acesso programático.
* O utilitário não recria níveis de proteção; apenas os remove.

## Teste
A estratégia não inclui testes automatizados dedicados porque executa funções utilitárias sem cálculos complexos. O teste manual pode ser realizado abrindo posições de amostra, anexando a estratégia e verificando se todas as ordens de proteção são canceladas após cada acionamento.

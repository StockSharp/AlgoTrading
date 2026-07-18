# Estratégia ExpICustomV1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo

A **Estratégia ExpICustomV1** é uma versão StockSharp do MetaTrader especialista `exp_iCustom_v1`. A estratégia lê sinais comerciais de uma instância de indicador configurável e reage a valores diferentes de zero nos buffers selecionados. Quando o buffer de compra é diferente de zero, a estratégia abre uma posição longa, enquanto o buffer de venda desencadeia uma entrada curta. A lógica protetora de stop-loss, take-profit, trailing e ponto de equilíbrio reproduz as opções de gerenciamento de dinheiro do especialista original.

> **Observação:** Somente a implementação C# é fornecida. Uma versão Python ainda não está disponível.

## Lógica de negociação

1. Assine o período primário especificado por **Tipo de vela** e processe apenas velas fechadas.
2. Instancie o indicador definido por **Indicator Name** e aplique os **Indicator Parameters** separados por barras (suporta pares `Name=Value` e valores numéricos ordenados).
3. Armazene as saídas finais do indicador em um buffer de histórico para que qualquer mudança possa ser acessada em velas posteriores.
4. Quando o valor do buffer de compra em **Indicator Shift** não é zero, a estratégia abre/mantém uma posição longa. Quando o buffer de venda é diferente de zero, a estratégia abre/mantém uma posição curta.
5. Se ambos os buffers retornarem valores diferentes de zero simultaneamente, os sinais se cancelarão para evitar entradas ambíguas.
6. Opcional **Close On Reverse** sai da posição atual antes de reagir ao sinal oposto.
7. A lógica de suspensão impõe um número mínimo de barras entre entradas consecutivas na mesma direção. O temporizador pode ser cancelado quando o sinal oposto dispara se **Cancelar sono** estiver ativado.
8. As posições são protegidas por stop-loss, take-profit, trailing stop opcional e bloqueio de ponto de equilíbrio. Todas as distâncias são expressas em faixas de preço.

## Configuração do indicador

* **Nome do indicador** – Nome completo do tipo ou nome abreviado do indicador StockSharp (por exemplo `SMA`, `MACD`, `BollingerBands`).
* **Parâmetros do Indicador** – Lista separada por barras aplicada ao indicador. Tanto `Length=14/Width=2` quanto valores ordenados como `14/2/0.7` são suportados.
* **Blocos de substituição** – Até cinco substituições permitem ajustar os valores dos parâmetros por índice durante a otimização, semelhante às entradas `Opt_X` no especialista original. Os índices são baseados em zero.

## Gestão de risco e dinheiro

* **Volume Base** – Valor enviado com cada ordem de mercado.
* **Stop Loss / Take Profit** – Distâncias absolutas em pontos do preço de entrada.
* **Trailing Stop** – Ativa após o lucro especificado e mantém a distância configurada do preço extremo.
* **Break Even** – Move o stop em direção ao preço de entrada após o lucro especificado e, opcionalmente, bloqueia pontos adicionais.

## Referência de parâmetro

| Parâmetro | Descrição |
|-----------|-------------|
| Tipo de vela | Prazo utilizado para avaliação do indicador e sinal. |
| Nome do indicador | Digite o nome da instância do indicador. |
| Parâmetros do Indicador | Lista de parâmetros do indicador separada por barras. |
| Comprar buffer / vender buffer | Índices de buffer que contêm os marcadores de compra/venda. |
| Mudança de indicador | Mudança histórica aplicada na leitura dos buffers do indicador. |
| Substituir blocos | Substitua posições de parâmetros específicos durante o tempo de execução. |
| Barras de dormir | Barras mínimas entre entradas na mesma direção. |
| Cancelar dormir | Reinicie o temporizador após um sinal oposto. |
| Fechar no sentido inverso | Fechar a posição existente quando o sinal oposto aparecer. |
| Máximo de pedidos / Máximo de compra / Máximo de venda | Soft caps que limitam o número de posições simultâneas. |
| Stop Loss / Take Profit | Distância em pontos para ordens de proteção. |
| Configurações de Trailing Stop | Parâmetros que controlam a ativação e a distância do trailing stop. |
| Configurações de ponto de equilíbrio | Parâmetros que controlam a ativação do ponto de equilíbrio e a distância de bloqueio. |
| Volume básico | Volume de cada entrada no mercado. |

## Uso

1. Adicione a estratégia ao seu contêiner de estratégia e defina **Segurança** e **Portfólio**.
2. Configure **Nome do indicador** e **Parâmetros do indicador** para corresponder ao indicador personalizado de destino.
3. Ajuste as configurações de risco (stop, take, trailing, break even) e o volume base do pedido.
4. Execute a estratégia. Ele assinará o prazo escolhido, avaliará os buffers do indicador e enviará ordens de mercado quando as condições forem atendidas.

## Limitações

* O indicador deve estar disponível como um tipo de indicador StockSharp. Os indicadores binários MetaTrader não podem ser carregados diretamente.
* Os modos de gestão de dinheiro que dependem da margem livre do corretor são simplificados para um volume base fixo.

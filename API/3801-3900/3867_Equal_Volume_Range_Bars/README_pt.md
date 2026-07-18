# Barras iguais de volume e alcance
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Barras iguais de volume e intervalo transportam o script MetaTrader 4 `equalvolumebars.mq4` para StockSharp. O script original gerava gráficos off-line cujas velas fechavam após um número fixo de ticks ou após o preço ter atravessado uma faixa de pontos configurável. A estratégia reproduz a mesma lógica de construção de velas dentro do ambiente StockSharp: ela escuta ticks ao vivo, opcionalmente pré-carrega velas M1 históricas e emite entradas de log detalhadas sempre que uma barra sintética é concluída.

## Lógica de construção de velas
* **Modos de operação duplos** – `EqualVolumeBars` fecha a barra quando o volume de ticks acumulado excede o limite configurado, enquanto `RangeBars` exige que o intervalo alto-baixo da vela (medido em etapas de preço de segurança) exceda o mesmo limite numérico.
* **Atualizações baseadas em ticks** – cada atualização de negociação atualiza a máxima, a mínima, o fechamento e o volume de ticks da vela atual. Quando o limite for excedido, a estratégia finaliza a vela anterior com as estatísticas existentes e inicia imediatamente uma nova barra com o tick atual como sua primeira entrada.
* **Semeadura de histórico de minutos (opcional)** – quando `FromMinuteHistory` está habilitado, a estratégia reproduz velas M1 finalizadas como uma sequência de ticks sintéticos (abertura → extremos intermediários → fechamento). Isso se aproxima da etapa de inicialização do gráfico off-line sem a necessidade de arquivos CSV externos.
* **Carimbos de data/hora monotônicos** – o construtor impõe carimbos de data/hora estritamente crescentes para que os consumidores de log ou módulos downstream possam carregar os dados sem encontrar chaves de tempo duplicadas.

## Parâmetros
* **Modo de trabalho** – seleciona entre `EqualVolumeBars` e `RangeBars` construção de velas.
* **Ticks In Bar** – número de ticks por vela (modo de volume igual) ou intervalo de pontos medido em etapas de preço (modo de intervalo).
* **Usar histórico de minutos** – permite a reprodução sintética de velas M1 finalizadas antes que os ticks ao vivo cheguem.
* **Tipo de vela de minuto** – assinatura de vela usada para a etapa de propagação histórica (o padrão é o período de um minuto).

## Notas adicionais
* A estratégia infere o tamanho do ponto de `Security.PriceStep` (voltando para `Security.MinPriceStep` ou `0.0001` quando nenhum metadado está disponível) para espelhar a constante `_Point` usada por MetaTrader.
* Em vez de gravar arquivos `.hst` e atualizar uma janela de gráfico, a porta C# registra cada vela concluída com dados OHLCV completos, facilitando a alimentação de outro componente ou a comparação de resultados com o construtor de gráficos offline MT4.
* Nenhum pedido é enviado; a aula se concentra exclusivamente na transformação de dados, assim como o script original.
* Somente a versão C# é fornecida. Uma versão e uma pasta do Python são omitidas intencionalmente de acordo com os requisitos de conversão.

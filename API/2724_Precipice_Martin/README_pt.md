# Estratégia Precipice Martin (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

A estratégia Precipice Martin é uma abordagem de grid mecânica que abre uma ordem a mercado no fechamento de cada vela processada. O consultor especializado original do MetaTrader 5 criava uma posição de compra e venda simétrica em cada nova barra e gerenciava as saídas usando offsets estáticos de stop-loss e take-profit expressos em pips. Operações perdedoras aumentavam o tamanho da próxima ordem por um multiplicador martingale, enquanto operações lucrativas reajustavam o tamanho da posição ao lote mínimo.

Este port em C# segue a mesma lógica de alto nível usando a API de alto nível do StockSharp. Para cada vela terminada a estratégia:

1. Atualiza as posições compradas e vendidas existentes e as fecha se o intervalo da vela penetrou o nível de stop-loss ou take-profit configurado.
2. Quando plana, alterna entre abrir uma posição de mercado comprada ou vendida (quando ambas as direções estão habilitadas) para emular o comportamento de entrada dupla do robô fonte enquanto permanece compatível com a contabilidade de posição líquida do StockSharp.
3. Aplica dimensionamento martingale opcional para que operações perdedoras consecutivas aumentem o volume pelo multiplicador configurado.
4. Calcula os alvos de stop-loss e take-profit a partir de distâncias de pips definidas pelo usuário que são traduzidas em offsets de preço absolutos com base no tamanho do tick do instrumento.

## Notas de Conversão

* O EA original abria uma posição comprada e vendida em cada nova barra quando ambos os toggles estavam habilitados. Como o StockSharp usa posições líquidas por padrão, a versão em C# alterna entre direções em oportunidades consecutivas para evitar aplanar instantaneamente a posição líquida. Isso ainda garante que ambos os lados do mercado sejam negociados ao longo do tempo.
* O gerenciamento de stop-loss e take-profit é realizado internamente verificando se o máximo/mínimo de uma vela teria acionado o nível correspondente. Quando um nível é atingido, a estratégia fecha a posição usando uma ordem a mercado e registra o lucro ou perda realizado para a lógica martingale.
* A validação de lotes replica a rotina `LotCheck` do MQL5 arredondando o volume calculado para o `VolumeStep` do exchange, aplicando os limites mínimo e máximo, e cancelando a ordem se o valor arredondado se tornar zero.
* A rotina martingale reflete `CalculateLot`: qualquer saída não lucrativa multiplica o tamanho da próxima ordem por `MartingaleCoefficient`, enquanto uma saída lucrativa reajusta o multiplicador para um.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| **Use Buy** | Habilita a abertura de posições compradas. |
| **Buy SL/TP (pips)** | Distância (em pips) usada tanto para o stop-loss como para o take-profit das operações compradas. Um valor de 0 desabilita as saídas para aquele lado. |
| **Use Sell** | Habilita a abertura de posições vendidas. |
| **Sell SL/TP (pips)** | Distância (em pips) usada tanto para o stop-loss como para o take-profit das operações vendidas. |
| **Use Martingale** | Alterna o dimensionamento de posição martingale. Quando desabilitado, cada ordem usa o tamanho de lote mínimo. |
| **Martingale Coefficient** | Multiplicador aplicado ao lote mínimo após cada operação não lucrativa. |
| **Candle Type** | Período das velas processadas pela estratégia. Por padrão a estratégia trabalha em barras de um minuto, mas qualquer período disponível pode ser selecionado. |

## Lógica de Trading

1. **Cálculo do Tamanho do Pip** – a estratégia deriva o valor do pip do tamanho do tick do instrumento. Para instrumentos cotados com pips fracionários (símbolos FX de 5 dígitos) o pip é considerado 10 ticks, correspondendo à implementação MT5.
2. **Seleção de Entrada** – se tanto `Use Buy` como `Use Sell` estão habilitados, a estratégia alterna entre entradas compradas e vendidas sempre que estiver plana. Se apenas uma direção estiver habilitada, todas as operações seguem essa direção. As entradas são acionadas imediatamente após a conclusão de uma vela e a estratégia estar online.
3. **Níveis de Stop/Take** – quando uma operação é aberta, o stop-loss e take-profit são armazenados como preços absolutos relativos à entrada usando a distância de pips selecionada. Um valor de zero desabilita ambos os níveis para aquela direção.
4. **Tratamento de Saídas** – em cada vela terminada os valores de máximo/mínimo são verificados. Se o mínimo viola o stop comprado ou o máximo viola o alvo comprado, a posição comprada é fechada. Para vendidos, a lógica é espelhada. As saídas são executadas com ordens a mercado usando o último volume registrado para aquela posição.
5. **Dimensionamento Martingale** – o volume da próxima ordem é igual ao lote mínimo do instrumento multiplicado pelo multiplicador martingale atual. Operações perdedoras (incluindo resultados de empate) multiplicam o multiplicador por `MartingaleCoefficient`; operações lucrativas o reajustam para um. O arredondamento de volume para o passo do exchange é aplicado antes de enviar a ordem.
6. **Verificações de Segurança** – se o volume arredondado estiver abaixo do lote mínimo do exchange, a ordem é ignorada, evitando erros de "fundos insuficientes" que o EA original tratava via `CheckVolume`.

## Diretrizes de Uso

1. Configure o período desejado em **Candle Type** para corresponder ao período do gráfico usado no MT5.
2. Ajuste as distâncias em pips para corresponder ao comportamento desejado de stop-loss e take-profit. Lembre-se de que os offsets são preços absolutos, então o stop real em moeda depende do símbolo.
3. Habilite ou desabilite o dimensionamento martingale de acordo com sua tolerância ao risco. Como o volume cresce exponencialmente após perdas consecutivas, aplique multiplicadores conservadores.
4. Implante a estratégia em um instrumento que forneça velas em tempo real. A estratégia requer barras completas para operar e não negociará em velas incompletas.
5. Monitore o uso de margem quando o martingale estiver ativo. A versão StockSharp alterna intencionalmente direções quando ambos os lados estão habilitados, de modo que apenas uma posição líquida esteja aberta em qualquer momento.

## Diferenças da Implementação MT5

* **Posições Líquidas** – a lógica de alternância substitui as entradas hedgeadas simultâneas do algoritmo original. Se uma conta de hedge verdadeiro for necessária, você pode executar duas instâncias da estratégia (uma com `Use Buy`, outra com `Use Sell`).
* **Colocação de Ordens** – as ordens protetoras não são colocadas no livro do exchange. Em vez disso, as saídas são executadas via ordens a mercado quando a estratégia detecta que o nível de stop ou take foi cruzado.
* **Varredura de Histórico** – o script MT5 recalculava o coeficiente martingale varrendo todo o histórico de negociações a cada tick. A versão em C# mantém o multiplicador incrementalmente para reduzir a sobrecarga enquanto preserva o comportamento.

## Aviso de Risco

Estratégias baseadas em martingale podem gerar posições muito grandes durante sequências de perdas, que podem exceder os limites de risco da conta. Sempre teste a estratégia em dados simulados antes da implantação ao vivo e certifique-se de que o multiplicador selecionado e as distâncias em pips sejam adequados para a volatilidade do instrumento negociado.

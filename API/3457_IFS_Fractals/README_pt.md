# SE Fractals
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
IFS Fractals é uma porta do script MetaTrader 5 `IFS_Fractals`. O especialista original renderiza um bitmap do sistema de função iterada (IFS) da "palavra fractal" aplicando repetidamente 28 transformações afins a uma nuvem de pontos. A versão StockSharp transforma o mesmo processo caótico em um oscilador direcional: a coordenada X dos pontos gerados é dimensionada, suavizada com uma média móvel exponencial (EMA) e interpretada como um medidor de momento que impulsiona entradas longas e curtas.

## Lógica estratégica
### Sistema de função iterado
* **Transformações afins** – cada vela finalizada aciona um lote de iterações (configuráveis). Durante cada iteração, uma das 28 transformadas é selecionada de acordo com os pesos de probabilidade originais (todos iguais a 35). A transformação atualiza o ponto atual `(x, y)` usando os coeficientes portados literalmente do código MQL5.
* **Tabela de probabilidades** – a estratégia pré-calcula uma matriz de probabilidade cumulativa uma vez no início, permitindo a seleção rápida da próxima transformação usando um único sorteio aleatório dentro da massa total de probabilidades.

### Construção de sinal
* **Normalização** – a coordenada X é dividida pelo mesmo fator de escala (`50` por padrão) que o script usou ao projetar o fractal no bitmap. Isto mantém o sinal em uma faixa numérica estável, independentemente do preço do instrumento.
* **EMA suavização** – a série normalizada alimenta um EMA cujo período é configurável. O EMA atua como um filtro passa-baixa que extrai a deriva dominante das iterações caóticas.
* **Lógica de entrada** – quando o EMA sobe acima do limite de entrada positivo, a estratégia abre ou reverte para uma posição longa. Simetricamente, quando o EMA cai abaixo do limite negativo, ele abre ou reverte para um curto.
* **Lógica de saída** – as posições compradas abertas saem quando o EMA volta para ou abaixo do limite de saída, enquanto as posições vendidas saem quando o EMA sobe novamente acima do limite de saída negativo. Isso cria uma banda de histerese que evita oscilações rápidas em torno de zero.

### Gestão de risco
* **Proteção de posição** – distâncias absolutas opcionais de stop-loss e take-profit podem ser habilitadas por meio de `StartProtection`. Um valor de `0` desativa o respectivo nível, correspondendo ao comportamento do script de origem que operou sem ordens de proteção.
* **Controle de volume** – as entradas utilizam um parâmetro fixo de volume de mercado. Qualquer exposição oposta existente é fechada antes de uma nova negociação ser aberta para manter uma posição direcional única.

## Parâmetros
* **Volume** – volume de mercado para novas entradas.
* **Tipo de vela** – período de tempo que orienta as iterações fractais (padrão: velas de 5 minutos).
* **Iterações** – número de iterações IFS processadas após cada vela concluída.
* **Escala** – divisor aplicado à coordenada X antes de alimentá-la no EMA.
* **Limite de entrada** – valor absoluto EMA necessário para abrir uma posição (positivo para posições longas, negativo espelhado para posições vendidas).
* **Limite de saída** – valor EMA que aciona saídas quando o sinal reverte para zero.
* **EMA Período** – período de suavização da média móvel exponencial aplicada ao sinal fractal normalizado.
* **Take Profit** – distância absoluta de take-profit; defina como `0` para desativar.
* **Stop Loss** – distância absoluta de stop-loss; defina como `0` para desativar.

## Notas adicionais
* Cada execução produz uma sequência de negociação diferente, a menos que uma semente aleatória determinística seja injetada pela modificação da fonte; isso reflete a aleatoriedade do script de renderização de bitmap original.
* A estratégia não requer quaisquer indicadores derivados do mercado. Todos os dados são gerados internamente a partir dos coeficientes IFS, portanto as velas subscritas simplesmente fornecem o tempo para as iterações.
* Nenhuma implementação Python está incluída neste pacote. Somente a estratégia C# está disponível em `CS/`.

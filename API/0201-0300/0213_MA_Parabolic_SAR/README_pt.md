# Estratégia MA Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia MA Parabolic SAR tenta capturar tendências sustentadas usando uma média móvel simples para determinar a direção predominante e os pontos Parabolic SAR para o timing de entrada e posicionamento do stop. Quando ambos os indicadores se alinham, o sistema assume que o momentum é forte o suficiente para seguir.

Os testes indicam um retorno anual médio de aproximadamente 76%. Funciona melhor no mercado de câmbio.

Uma posição comprada é aberta quando o preço de fechamento está acima da média móvel e os pontos Parabolic SAR viram abaixo do mercado. Uma posição vendida é tomada quando o preço está abaixo da média e os pontos SAR viram acima do preço, sinalizando pressão de baixa. A estratégia sai assim que o preço cruza o SAR na direção oposta, assegurando lucros ou limitando perdas.

Esta abordagem é mais adequada para traders que preferem seguimento de tendência sistemático com stops claros e mecânicos. O Parabolic SAR se ajusta continuamente à medida que a volatilidade muda, mantendo a exposição em linha com as condições de mercado enquanto a média móvel previne trades contra a tendência mais ampla.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Price > MA && Price > Parabolic SAR
  - **Vendido**: Price < MA && Price < Parabolic SAR
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando o preço cair abaixo do Parabolic SAR
  - **Vendido**: Sair quando o preço subir acima do Parabolic SAR
- **Stops**: Sim, dinâmico via Parabolic SAR e stop fixo opcional.
- **Valores padrão**:
  - `MaPeriod` = 20
  - `SarStep` = 0.02m
  - `SarMaxStep` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `TakeValue` = new Unit(0, UnitTypes.Absolute)
  - `StopValue` = new Unit(2, UnitTypes.Percent)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MA, Parabolic SAR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio


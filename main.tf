# 1. Tell Terraform we want to use AWS in the US-East region
provider "aws" {
  region = "us-east-1"
}

# 2. FIREWALL: Create a "Security Group" to protect our server
resource "aws_security_group" "documark_sg" {
  name        = "documark-web-sg"
  description = "Allow web and SSH traffic"

  # Allow HTTP web traffic (Port 80)
  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # Allow Next.js traffic (Port 3000)
  ingress {
    from_port   = 3000
    to_port     = 3000
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # Allow SSH to securely log into the server
  ingress {
    from_port   = 22
    to_port     = 22
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # Allow the server to download internet updates
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

# 3. SERVER: Order a free-tier Ubuntu Linux virtual machine
resource "aws_instance" "documark_server" {
  ami           = "ami-080e1f13689e07408" # Official Ubuntu 24.04 ID
  instance_type = "t3.micro"              # AWS Free Tier!
  
  vpc_security_group_ids = [aws_security_group.documark_sg.id]

  # AUTOMATION: A startup script that runs the moment the server turns on
  # This tells the server to install Docker and Git immediately
# AUTOMATION: Install tools, download code, and start the app automatically
  user_data = <<-EOF
              #!/bin/bash
              # 1. Install Dependencies
              sudo apt-get update -y
              sudo apt-get install -y docker.io docker-compose git
              sudo systemctl start docker
              sudo systemctl enable docker
              sudo usermod -aG docker ubuntu

              # 2. Fetch the Code & Run the App
              cd /home/ubuntu
              git clone https://github.com/R20T04G/DocuMark
              cd DocuMark
              sudo docker-compose up -d
              EOF

  tags = {
    Name = "DocuMark-Production-Server"
  }
}

# 4. OUTPUT: Tell Terraform to print the public IP address so we can see our app
output "server_public_ip" {
  value = aws_instance.documark_server.public_ip
}